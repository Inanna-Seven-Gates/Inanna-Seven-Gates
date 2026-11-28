namespace Inanna.Levels
{
    /// <summary>Algo que tranca e destranca uma passagem (grade, porta, barreira de energia).</summary>
    public interface ILockable
    {
        bool IsLocked { get; }

        /// <summary>Tranca com animação.</summary>
        void Lock();

        /// <summary>Destranca com animação.</summary>
        void Unlock();

        /// <summary>Aplica o estado na hora, sem animação. Serve para carregar save e para o validador de percurso.</summary>
        void SetLockedImmediate(bool locked);
    }
}
