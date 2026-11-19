namespace Inanna.Levels
{
    /// <summary>
    /// Algo que um gatilho (alavanca, placa de pressão, fim de luta) pode acionar: ponte, porta, plataforma.
    /// O gatilho depende só desta interface e não sabe o que o alvo faz.
    /// </summary>
    public interface IActivatable
    {
        bool IsActivated { get; }

        /// <summary>Aciona com animação e eventos.</summary>
        void Activate();

        /// <summary>Aplica o estado na hora, sem animação nem eventos. Serve para carregar save e para o validador de percurso.</summary>
        void SetActivatedImmediate(bool activated);
    }
}
