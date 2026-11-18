using UnityEngine;
using UnityEngine.InputSystem;

namespace Inanna.Player
{
    /// <summary>
    /// Única classe que conhece o Input System. O resto do jogo lê daqui.
    /// As ações são criadas por código (sem asset) para facilitar o início do projeto.
    /// Os bindings de gamepad já estão aqui: quando formos suportar controle, só ajustamos a lógica de mira.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        InputAction _move, _point, _attack, _throw, _dodge, _interact, _inventory, _weaponScroll;

        // ---- Leitura ----
        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 PointerPosition => _point.ReadValue<Vector2>();

        public bool AttackPressed => _attack.WasPressedThisFrame();
        public bool AttackHeld => _attack.IsPressed();

        public bool ThrowPressed => _throw.WasPressedThisFrame();
        public bool ThrowHeld => _throw.IsPressed();
        public bool ThrowReleased => _throw.WasReleasedThisFrame();

        public bool DodgePressed => _dodge.WasPressedThisFrame();
        public bool InteractPressed => _interact.WasPressedThisFrame();
        public bool InventoryPressed => _inventory.WasPressedThisFrame();

        /// <summary>+1 / -1 / 0 conforme o scroll do mouse.</summary>
        public int WeaponScroll => (int)Mathf.Sign(_weaponScroll.ReadValue<Vector2>().y) * 
                                    (Mathf.Abs(_weaponScroll.ReadValue<Vector2>().y) > 0.01f ? 1 : 0);

        void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _point = new InputAction("Point", InputActionType.Value, "<Mouse>/position", expectedControlType: "Vector2");

            _attack = new InputAction("Attack", InputActionType.Button);
            _attack.AddBinding("<Mouse>/leftButton");
            _attack.AddBinding("<Gamepad>/buttonWest");

            _throw = new InputAction("Throw", InputActionType.Button);
            _throw.AddBinding("<Mouse>/rightButton");
            _throw.AddBinding("<Gamepad>/rightTrigger");

            _dodge = new InputAction("Dodge", InputActionType.Button);
            _dodge.AddBinding("<Keyboard>/space");
            _dodge.AddBinding("<Gamepad>/buttonSouth");

            _interact = new InputAction("Interact", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/c");
            _interact.AddBinding("<Gamepad>/buttonNorth");

            _inventory = new InputAction("Inventory", InputActionType.Button);
            _inventory.AddBinding("<Keyboard>/e");
            _inventory.AddBinding("<Gamepad>/start");

            _weaponScroll = new InputAction("WeaponScroll", InputActionType.Value, "<Mouse>/scroll", expectedControlType: "Vector2");
        }

        void OnEnable()
        {
            _move.Enable(); _point.Enable(); _attack.Enable(); _throw.Enable();
            _dodge.Enable(); _interact.Enable(); _inventory.Enable(); _weaponScroll.Enable();
        }

        void OnDisable()
        {
            _move.Disable(); _point.Disable(); _attack.Disable(); _throw.Disable();
            _dodge.Disable(); _interact.Disable(); _inventory.Disable(); _weaponScroll.Disable();
        }

        void OnDestroy()
        {
            _move?.Dispose(); _point?.Dispose(); _attack?.Dispose(); _throw?.Dispose();
            _dodge?.Dispose(); _interact?.Dispose(); _inventory?.Dispose(); _weaponScroll?.Dispose();
        }
    }
}
