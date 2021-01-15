using System.Diagnostics;

namespace SingleBoostr.Client
{
    internal class IdlingAppData
    {
        public Process IdlingProcess { get; set; }
        public int AppId { get; set; }

        public IdlingAppData(Process p, int id)
        {
            IdlingProcess = p;
            AppId = id;
        }
    }
}
