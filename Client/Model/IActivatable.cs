using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Model
{
    interface IActivatable
    {
        bool Activated { get; }

        void Activate();

        void Deactivate();
    }
}
