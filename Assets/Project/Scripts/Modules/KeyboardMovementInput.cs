using UnityEngine;
using UnityEngine.InputSystem;

namespace BrmnModules.Input
{
    public class KeyboardMovementInput : MonoBehaviour, IMovementInput
    {
        private Vector2 _moveInput;

        public Vector2 MovementDirection => _moveInput;

        // Connect to Input System Action (using PlayerInput component)
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }
    }
}