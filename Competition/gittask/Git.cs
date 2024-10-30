using System;
using System.Collections.Generic;
//using System.Linq;
using Files = System.Collections.Generic.Dictionary<int, GitTask.File>;

namespace GitTask
{
    public class File
    {
        public int Value { get; set; }
    }

    public class Git
    {
        private Dictionary<int, Files> history;
        private Files files;
        //private Files lastCommit => history.LastOrDefault().Value ?? new Files();

        public Git(int filesCount)
        {
            history = new Dictionary<int, Files>();
            files = new Files(filesCount);
        }

        public void Update(int fileNumber, int value)
        {
            if (files.ContainsKey(fileNumber) && files[fileNumber].Value == value)
                return;

            files[fileNumber] = new File { Value = value };
        }

        public int Commit()
        {
            //if (files.Except(lastCommit).Any() || lastCommit.Except(files).Any())
            history.Add(history.Count, new Files(files));
            return history.Count - 1;
        }

        public int Checkout(int commitNumber, int fileNumber)
        {
            if (commitNumber < history.Count)
            {
                if (history[commitNumber].ContainsKey(fileNumber))
                    return history[commitNumber][fileNumber].Value;
                else
                    return 0;
            }
            else
                throw new ArgumentException();
        }
    }
}