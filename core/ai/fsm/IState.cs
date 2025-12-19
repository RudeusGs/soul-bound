public interface IState
{
    string Name { get; }
    void Enter();
    void Exit();
    void Tick(double delta);
}
