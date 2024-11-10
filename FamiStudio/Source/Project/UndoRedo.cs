using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;

namespace FamiStudio
{
    public enum TransactionScope
    {
        Project,
        ProjectNoDPCMSamples,
        DPCMSample,
        Song,
        Channel,
        Pattern,
        Instrument,
        Arpeggio,
        Application,
        Max
    };

    public enum TransactionFlags
    {
        None            = 0,
        StopAudio       = 1,
        RecreatePlayers = 2,
        RecreateStreams = 4 // Should only be used along with RecreatePlayers.
    };

    public class Transaction
    {
        private FamiStudio app;
        private Project project;
        private TransactionScope scope;
        private TransactionFlags flags;
        private int objectId;
        private byte[] stateBefore;
        private byte[] stateAfter;
        private int subIdx;

        public TransactionScope Scope { get => scope; private set => scope = value; }
        public TransactionFlags Flags { get => flags; }
        public int ObjectId { get => objectId; private set => objectId = value; }
        public byte[] StateBefore { get => stateBefore; private set => stateBefore = value; }
        public byte[] StateAfter { get => stateAfter; private set => stateAfter = value; }

        public Transaction(Project project, FamiStudio app, TransactionScope scope, TransactionFlags flags, int objectId, int subIdx = -1)
        {
            this.project = project;
            this.app = app;
            this.scope = scope;
            this.flags = flags;
            this.objectId = objectId;
            this.subIdx = subIdx;
        }

        public void Begin()
        {
            stateBefore = CaptureState();
        }

        public void End()
        {
            stateAfter = CaptureState();
        }

        public void Undo(bool serializeAppState = true)
        {
            RestoreState(stateBefore, serializeAppState);
        }

        public void Redo()
        {
            RestoreState(stateAfter);
        }

        public bool IsEnded
        {
            get { return stateAfter != null; }
        }

        private void Serialize(ProjectBuffer buffer, bool serializeAppState = true)
        {
            switch (scope)
            {
                case TransactionScope.Project:
                    project.Serialize(buffer);
                    break;
                case TransactionScope.ProjectNoDPCMSamples:
                    project.Serialize(buffer, false);
                    break;
                case TransactionScope.DPCMSample:
                    project.GetSample(objectId).Serialize(buffer);
                    break;
                case TransactionScope.Instrument:
                    project.GetInstrument(objectId).Serialize(buffer);
                    break;
                case TransactionScope.Pattern:
                    project.GetPattern(objectId).Serialize(buffer);
                    break;
                case TransactionScope.Channel:
                    project.GetSong(objectId).Channels[subIdx].Serialize(buffer);
                    break;
                case TransactionScope.Song:
                    project.GetSong(objectId).Serialize(buffer);
                    break;
                case TransactionScope.Arpeggio:
                    project.GetArpeggio(objectId).Serialize(buffer);
                    break;
                case TransactionScope.Application:
                    break;
            }

            Theme.Serialize(buffer);

            if (serializeAppState)
                app.Serialize(buffer);
        }

        private byte[] CaptureState()
        {
            var buffer = new ProjectSaveBuffer(project, ProjectBufferFlags.UndoRedo);
            Serialize(buffer);
            return Compression.CompressBytes(buffer.GetBuffer(), CompressionLevel.Fastest);
        }

        private void RestoreState(byte[] state, bool serializeAppState = true)
        {
            var buffer = new ProjectLoadBuffer(project, Compression.DecompressBytes(state), Project.Version, ProjectBufferFlags.UndoRedo);
            Serialize(buffer, serializeAppState);
        }
    };

    public class UndoRedoManager
    {
        public delegate void UndoRedoDelegate(TransactionScope scope, TransactionFlags flags);
        public delegate void UpdatedDelegate();

        public event UndoRedoDelegate PreUndoRedo;
        public event UndoRedoDelegate PostUndoRedo;
        public event UndoRedoDelegate TransactionBegan;
        public event UndoRedoDelegate TransactionEnded;
        public event UpdatedDelegate  Updated;

        private FamiStudio app;
        private Project project;
        private List<Transaction> transactions = new List<Transaction>();
        private int index = 0;
        private int saveIndex = 0;
        private bool forceDirty;

        public UndoRedoManager(Project proj, FamiStudio app) 
        {
            this.project = proj;
            this.app = app;
        }

        public void ForceDirty()
        {
            forceDirty = true;
        }

        public void BeginTransaction(TransactionScope scope, TransactionFlags flags)
        {
            BeginTransaction(scope, -1, -1, flags);
        }

        public void BeginTransaction(TransactionScope scope, int objectId = -1, int subIdx = -1, TransactionFlags flags = TransactionFlags.None)
        {
            Debug.Assert(transactions.Count == 0 || transactions.Last().IsEnded);

            if (index != transactions.Count)
            {
                transactions.RemoveRange(index, transactions.Count - index);
            }

            var trans = new Transaction(project, app, scope, flags, objectId, subIdx);
            transactions.Add(trans);
            trans.Begin();
            index++;
            TransactionBegan?.Invoke(scope, flags);
            if (!Platform.IsMobile) // MATTT
                project.ValidateIntegrity(); 
        }

        public void EndTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            var trans = transactions[transactions.Count - 1];
            Debug.Assert(trans.StateAfter == null);
            trans.End();
            TransactionEnded?.Invoke(trans.Scope, trans.Flags);
            Updated?.Invoke();
            if (!Platform.IsMobile) // MATTT
                project.ValidateIntegrity();
        }

        public void AbortTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            var trans = transactions[transactions.Count - 1];
            transactions.RemoveAt(transactions.Count - 1);
            index--;
            TransactionEnded?.Invoke(trans.Scope, trans.Flags);
            Updated?.Invoke();
            project.ValidateIntegrity();
        }

        public void AbortOrEndTransaction(bool success)
        {
            if (success)
                EndTransaction();
            else
                AbortTransaction();
        }

        public void RestoreTransaction(bool serializeAppState = true)
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            var trans = transactions[transactions.Count - 1];

            // Cant restore these transactions for the moment as we dont notify the app
            // which hold pointers to the current song. This can lead to a situation where
            // the app is pointing to a old/dangling copy of the song.
            Debug.Assert(
                trans.Scope != TransactionScope.Project &&
                trans.Scope != TransactionScope.ProjectNoDPCMSamples);

            trans.Undo(serializeAppState);
            Updated?.Invoke();
            project.ValidateIntegrity();
        }

        public bool HasTransactionInProgress
        {
            get
            {
                return transactions.Count() > 0 && !transactions.Last().IsEnded;
            }
        }

        public bool NeedsSaving
        {
            get
            {
                return saveIndex != index || forceDirty;
            }
        }

        public TransactionScope UndoScope
        {
            get { return index > 0 ? transactions[index - 1] .Scope : TransactionScope.Max; }
        }

        public TransactionScope RedoScope
        {
            get { return index < transactions.Count ? transactions[index].Scope : TransactionScope.Max; }
        }

        public void NotifySaved()
        {
            saveIndex = index;
            forceDirty = false;
        }

        public void Undo()
        {
            if (index > 0)
            {
                var trans = transactions[index - 1];
                PreUndoRedo?.Invoke(trans.Scope, trans.Flags);
                trans.Undo();
                index--;
                PostUndoRedo?.Invoke(trans.Scope, trans.Flags);
                Updated?.Invoke();
            }
        }

        public void Redo()
        {
            if (index < transactions.Count)
            {
                index++;
                var trans = transactions[index - 1];
                PreUndoRedo?.Invoke(trans.Scope, trans.Flags);
                trans.Redo();
                PostUndoRedo?.Invoke(trans.Scope, trans.Flags);
                Updated?.Invoke();
            }
        }

        public void Clear()
        {
            Debug.Assert(transactions.Count == 0 || transactions.Last().IsEnded);
            index = 0;
            forceDirty = false;
            transactions.Clear();
            Updated?.Invoke();
        }
    }
}
