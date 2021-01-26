using System.Diagnostics;

namespace SingleBoostr.Client
{
    internal class IdlingAppData
    {
        public Process IdlingProcess { get; set; }
        public uint AppId { get; set; }

        public IdlingAppData(Process p, uint id)
        {
            IdlingProcess = p;
            AppId = id;
        }
    }
}
