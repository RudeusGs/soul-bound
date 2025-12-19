namespace soulbound.core.ai.fsm
{
    public class StateMachine
    {
        private IState _current;
        public string CurrentName => _current?.Name ?? "(none)";

        public void Change(IState next)
        {
            if (next == null || ReferenceEquals(_current, next)) return;
            _current?.Exit();
            _current = next;
            _current.Enter();
        }

        public void Tick(double delta) => _current?.Tick(delta);
    }

}
