namespace Milease.Milease.Exception
{
    public class MilAwaitAniPausedException : System.Exception
    {
        public MilAwaitAniPausedException() 
            : base("The animation that you are waiting for is paused.")
        {

        }
    }
}
