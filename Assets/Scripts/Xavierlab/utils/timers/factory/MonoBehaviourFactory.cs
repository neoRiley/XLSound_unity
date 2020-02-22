namespace XavierLab
{
    public class MonoBehaviourFactory : TimerFactory
    {
        public override BaseTimer Create() => new MonoBehaviourTimer();
    }
}
