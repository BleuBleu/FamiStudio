using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;

namespace FamiStudio
{
    public enum TransactionScope
    {
        Project,
        ProjectProperties,
        DCPMSamples,
        Song,
        Channel,
        Pattern,
        Instrument,
        Arpeggio,
        Application,
        Max
    };

    public class Transaction
    {
        private FamiStudio app;
        private Project project;
        private TransactionScope scope;
        private int objectId;
        private byte[] stateBefore;
        private byte[] stateAfter;
        private int subIdx;

        public TransactionScope Scope { get => scope; private set => scope = value; }
        public int ObjectId { get => objectId; private set => objectId = value; }
        public byte[] StateBefore { get => stateBefore; private set => stateBefore = value; }
        public byte[] StateAfter { get => stateAfter; private set => stateAfter = value; }

        public Transaction(Project project, FamiStudio app, TransactionScope scope, int objectId, int subIdx = -1)
        {
            this.project = project;
            this.app = app;
            this.scope = scope;
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
                case TransactionScope.ProjectProperties:
                    project.SerializeState(buffer);
                    break;
                case TransactionScope.DCPMSamples:
                    project.SerializeDPCMState(buffer);
                    break;
                case TransactionScope.Instrument:
                    project.GetInstrument(objectId).SerializeState(buffer);
                    break;
                case TransactionScope.Pattern:
                    project.GetPattern(objectId).SerializeState(buffer);
                    break;
                case TransactionScope.Channel:
                    project.GetSong(objectId).Channels[subIdx].SerializeState(buffer);
                    break;
                case TransactionScope.Song:
                    project.GetSong(objectId).SerializeState(buffer);
                    break;
                case TransactionScope.Arpeggio:
                    project.GetArpeggio(objectId).SerializeState(buffer);
                    break;
                case TransactionScope.Application:
                    break;
            }

            if (serializeAppState)
                app.SerializeState(buffer); 
        }

        private byte[] CaptureState()
        {
            var buffer = new ProjectSaveBuffer(project, true);
            Serialize(buffer);
            return Compression.CompressBytes(buffer.GetBuffer(), CompressionLevel.Fastest);
        }

        private void RestoreState(byte[] state, bool serializeAppState = true)
        {
            var buffer = new ProjectLoadBuffer(project, Compression.DecompressBytes(state), Project.Version, true);
            Serialize(buffer, serializeAppState);
        }
    };

    public class UndoRedoManager
    {
        public delegate void UndoRedoDelegate(TransactionScope scope);
        public delegate void UpdatedDelegate();

        public event UndoRedoDelegate PreUndoRedo;
        public event UndoRedoDelegate PostUndoRedo;
        public event UpdatedDelegate  Updated;

        private FamiStudio app;
        private Project project;
        private List<Transaction> transactions = new List<Transaction>();
        private int index = 0;

        public UndoRedoManager(Project proj, FamiStudio app) 
        {
            this.project = proj;
            this.app = app;
        }

        public void BeginTransaction(TransactionScope scope, int objectId = -1, int subIdx = -1)
        {
            Debug.Assert(transactions.Count == 0 || transactions.Last().IsEnded);

            if (index != transactions.Count)
            {
                transactions.RemoveRange(index, transactions.Count - index);
            }

            var trans = new Transaction(project, app, scope, objectId, subIdx);
            transactions.Add(trans);
            trans.Begin();
            index++;
            project.Validate();
        }

        public void EndTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            var trans = transactions[transactions.Count - 1];
            Debug.Assert(trans.StateAfter == null);
            trans.End();
            Updated?.Invoke();
            project.Validate();
        }

        public void AbortTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            transactions.RemoveAt(transactions.Count - 1);
            index--;
            Updated?.Invoke();
            project.Validate();
        }

        public void RestoreTransaction(bool serializeAppState = true)
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            transactions[transactions.Count - 1].Undo(serializeAppState);
            project.Validate();
        }

        public bool HasTransactionInProgress
        {
            get
            {
                return transactions.Count() > 0 && !transactions.Last().IsEnded;
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

        public void Undo()
        {
            if (index > 0)
            {
                var trans = transactions[index - 1];
                PreUndoRedo?.Invoke(trans.Scope);
                trans.Undo();
                index--;
                PostUndoRedo?.Invoke(trans.Scope);
                Updated?.Invoke();
            }
        }

        public void Redo()
        {
            if (index < transactions.Count)
            {
                index++;
                var trans = transactions[index - 1];
                PreUndoRedo?.Invoke(trans.Scope);
                trans.Redo();
                PostUndoRedo?.Invoke(trans.Scope);
                Updated?.Invoke();
            }
        }

        public void Clear()
        {
            Debug.Assert(transactions.Count == 0 || transactions.Last().IsEnded);
            index = 0;
            transactions.Clear();
            Updated?.Invoke();
        }
    }
}
