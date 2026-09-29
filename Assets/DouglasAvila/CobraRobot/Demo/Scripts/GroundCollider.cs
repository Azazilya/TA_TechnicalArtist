using UnityEngine;

namespace DouglasAvila.CobraRobot.Scripts
{
    public class GroundCollider : MonoBehaviour
    {
        private int _collisionCount = 0;
        private float _disableTimer = 0f;

        private void OnEnable()
        {
            _collisionCount = 0;
        }

        public bool IsColliding()
        {
            if (_disableTimer > 0)
                return false;
            return _collisionCount > 0;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            _collisionCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            _collisionCount--;
        }
        
        void Update()
        {
            _disableTimer -= Time.deltaTime;
        }

        public void Disable(float duration)
        {
            _disableTimer = duration;
        }
    }
}
