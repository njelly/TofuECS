using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tofunaut.TofuECS
{
    public class Frame
    {
        public int Number { get; private set; }

        private readonly Simulation _sim;

        public Frame(Simulation sim)
        {
            _sim = sim;
            Number = -1;
        }

        public void Reset(Frame prevFrame)
        {
            Number = prevFrame.Number + 1;
        }
    }
}
