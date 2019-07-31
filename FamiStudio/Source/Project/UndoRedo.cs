using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    public enum TransactionScope
    {
        Project,
        DCPMSamples,
        Song,
        Pattern,
        Instrument,
        Max
    };

    public class Transaction
    {
        private FamiStudioForm form;
        private Project project;
        private TransactionScope scope;
        private int objectId;
        private byte[] stateBefore;
        private byte[] stateAfter;

        public TransactionScope Scope { get => scope; private set => scope = value; }
        public int ObjectId { get => objectId; private set => objectId = value; }
        public byte[] StateBefore { get => stateBefore; private set => stateBefore = value; }
        public byte[] StateAfter { get => stateAfter; private set => stateAfter = value; }

        public Transaction(Project project, FamiStudioForm form, TransactionScope scope, int objectId)
        {
            this.project = project;
            this.form = form;
            this.scope = scope;
            this.objectId = objectId;
        }

        public void Begin()
        {
            stateBefore = CaptureState();
        }

        public void End()
        {
            stateAfter = CaptureState();
        }

        public void Undo()
        {
            RestoreState(stateBefore);
        }

        public void Redo()
        {
            RestoreState(stateAfter);
        }

        public bool IsEnded
        {
            get { return stateAfter != null; }
        }

        private void Serialize(ProjectBuffer buffer)
        {
            switch (scope)
            {
                case TransactionScope.Project:
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
                case TransactionScope.Song:
                    project.GetSong(objectId).SerializeState(buffer);
                    break;
            }

            form.SerializeState(buffer); 
        }

        private byte[] CaptureState()
        {
            var buffer = new ProjectSaveBuffer(project, true);
            Serialize(buffer);
            return Compression.CompressBytes(buffer.GetBuffer(), CompressionLevel.Fastest);
        }

        private void RestoreState(byte[] state)
        {
            var buffer = new ProjectLoadBuffer(project, Compression.DecompressBytes(state), Project.Version, true);
            Serialize(buffer);
        }
    };

    public class UndoRedoManager
    {
        public delegate void UndoRedoDelegate();
        public event UndoRedoDelegate Updated;

        private FamiStudioForm mainForm;
        private Project project;
        private List<Transaction> transactions = new List<Transaction>();
        private int index = 0;

        public UndoRedoManager(Project proj, FamiStudioForm form) 
        {
            project = proj;
            mainForm = form;
        }

        public void BeginTransaction(TransactionScope scope, int objectId = -1)
        {
            Debug.Assert(transactions.Count == 0 || transactions.Last().IsEnded);

            if (index != transactions.Count)
            {
                transactions.RemoveRange(index, transactions.Count - index);
            }

            var trans = new Transaction(project, mainForm, scope, objectId);
            transactions.Add(trans);
            trans.Begin();
            index++;
        }

        public void EndTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            var trans = transactions[transactions.Count - 1];
            Debug.Assert(trans.StateAfter == null);
            trans.End();
            Updated?.Invoke();
        }

        public void AbortTransaction()
        {
            Debug.Assert(!transactions.Last().IsEnded);
            Debug.Assert(index == transactions.Count);
            transactions.RemoveAt(transactions.Count - 1);
            index--;
            Updated?.Invoke();
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
                transactions[index - 1].Undo();
                index--;
                Updated?.Invoke();
            }
        }

        public void Redo()
        {
            if (index < transactions.Count)
            {
                index++;
                transactions[index - 1].Redo();
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
