using UnityEngine;

public class CameraController : MonoBehaviour {
    private const float CAMERA_SPEED = 10f;

    // Update is called once per frame
    void Update() {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) {
            transform.position += transform.forward * CAMERA_SPEED * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)) {
            transform.position -= transform.forward * CAMERA_SPEED * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) {
            transform.position -= transform.right * CAMERA_SPEED * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D)) {
            transform.position += transform.right * CAMERA_SPEED * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftControl)) {
            transform.position -= transform.up * CAMERA_SPEED * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Space)) {
            transform.position += transform.up * CAMERA_SPEED * Time.deltaTime;
        }        

        // Look around with mouse holding right mouse button
        if (Input.GetMouseButton(1)) {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            transform.Rotate(Vector3.up * mouseX);
            transform.Rotate(Vector3.left * mouseY);
        }
        // lock z rotation
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
    }
}
