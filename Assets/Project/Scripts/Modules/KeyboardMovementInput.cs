using UnityEngine;
using UnityEngine.InputSystem;

namespace BrmnModules.Input
{
    public class KeyboardMovementInput : MonoBehaviour, IMovementInput
    {
        private Vector2 _moveInput;
        public Vector2 MovementDirection => _moveInput;

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }
    }
}