using UnityEngine;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private Camera _camera;
        private float _targetOrthoSize;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetOrthoSize = _camera.orthographicSize;
        }

        private void Update()
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + Input.mouseScrollDelta.y * 0.5f, 1, 34);

            var moveInput = new Vector2(Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1f : 0f,
                Input.GetKey(KeyCode.S) ? -1 : Input.GetKey(KeyCode.W) ? 1f : 0f);

            var newPos = transform.position + (Vector3)moveInput * Time.deltaTime * _camera.orthographicSize * 2f;
            newPos = new Vector3(Mathf.Clamp(newPos.x, 0f, 64f), Mathf.Clamp(newPos.y, 0f, 64f), newPos.z);
            transform.position = newPos;
        }
    }
}