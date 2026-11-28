namespace Inanna.Levels
{
    /// <summary>
    /// Algo que dispara uma vez a cada acionamento e pode disparar de novo (ex.: lançador de flechas).
    /// Diferente de <see cref="IActivatable"/>, que muda de estado uma vez só (ponte abaixada, porta aberta).
    /// </summary>
    public interface ITriggerable
    {
        void Trigger();
    }
}
