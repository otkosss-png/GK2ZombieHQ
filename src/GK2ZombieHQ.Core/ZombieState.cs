namespace GK2ZombieHQ.Core
{
    // Где находится зомби. OffWorld = данные в сейве есть, а тела в мире нет
    // (на столе воскрешения / в хранилище / пропал из-за бага — "призрак").
    public enum ZombieState
    {
        Working,
        Free,
        Lying,
        InHands,
        OffWorld
    }
}
