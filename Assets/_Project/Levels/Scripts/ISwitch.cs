using System;

namespace Inanna.Levels
{
    /// <summary>Algo que liga uma vez e avisa: alavanca, chave pega, placa de pressão.</summary>
    public interface ISwitch
    {
        bool IsActivated { get; }

        /// <summary>Disparado sempre que o estado muda (true = ligado).</summary>
        event Action<bool> StateChanged;

        /// <summary>Aplica o estado na hora, sem efeitos de jogo. Serve para carregar save e para o validador.</summary>
        void SetActivatedImmediate(bool activated);
    }
}
