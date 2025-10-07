public interface ISoldierState
{
    void EnterState(Soldier soldier);
    void UpdateState(Soldier soldier);
    void ExitState(Soldier soldier);
}