//http://answers.unity3d.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html

public class UnityThreadJob
{
    private bool unsafeIsDone = false;
    private object isDoneLock = new object();
    private System.Threading.Thread thread = null;
    public bool IsDone
    {
        get
        {
            bool tmp;
            lock (isDoneLock)
            {
                tmp = unsafeIsDone;
            }
            return tmp;
        }
        private set
        {
            lock (isDoneLock)
            {
                unsafeIsDone = value;
            }
        }
    }

    public void Start()
    {
        thread = new System.Threading.Thread(Run);
        thread.Start();
    }
    public void Abort()
    {
        thread.Abort();
    }

    protected virtual void ThreadFunction() { }

    private void Run()
    {
        ThreadFunction();
        IsDone = true;
    }
}