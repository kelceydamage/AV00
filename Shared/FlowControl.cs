namespace AV00.Shared
{
    public enum EnumExecutionMode
    {
        Blocking,
        NonBlocking,
        Override
    }

    public class CustomCancellationToken
    {
        public bool IsCancellationRequested
        {
            get => isCancellationRequested;
            set => isCancellationRequested = value;
        }
        private bool isCancellationRequested = false;
    }
}
