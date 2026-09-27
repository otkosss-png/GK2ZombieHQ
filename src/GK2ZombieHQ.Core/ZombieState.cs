namespace GK2ZombieHQ.Core
{
    // Где находится зомби. OffWorld = данные в сейве есть, а тела в мире нет
    // (пропал из-за бага). OnTable = тело на паллете/столе воскрешения, InChoir = в хоре/на органе.
    public enum ZombieState
    {
        Working,
        Free,
        Lying,
        InHands,
        OnTable,
        InChoir,
        OffWorld
    }
}
