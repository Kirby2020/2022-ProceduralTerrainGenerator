using UnityEngine;

public class CameraController : MonoBehaviour {
    private const float CAMERA_SPEED = 10f;
    private const float CAMERA_SPRINT_SPEED = 30f;
    private const float CAMERA_ROTATION_SPEED = 80f;

    // Update is called once per frame
    void Update() {
        var cameraSpeed = Input.GetKey(KeyCode.LeftShift) ? CAMERA_SPRINT_SPEED : CAMERA_SPEED;
        
        // Move camera
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z)) {
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z);
            transform.position += forward * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S)) {
            Vector3 backward = new Vector3(-transform.forward.x, 0, -transform.forward.z);
            transform.position += backward * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) {
            Vector3 left = new Vector3(-transform.right.x, 0, -transform.right.z);
            transform.position += left * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D)) {
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z);
            transform.position += right * cameraSpeed * Time.deltaTime;
        }        
        if (Input.GetKey(KeyCode.Space)) {
            transform.position += Vector3.up * cameraSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftControl)) {
            transform.position += Vector3.down * cameraSpeed * Time.deltaTime;
        }

        // Look around with mouse holding right mouse button or arrow keys
        if (Input.GetMouseButton(1) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)) {
            float mouseX = Input.GetAxis("Mouse X") * 3;
            float mouseY = Input.GetAxis("Mouse Y") * 3;

            if (Input.GetKey(KeyCode.UpArrow)) {
                mouseY = 1;
            }
            if (Input.GetKey(KeyCode.DownArrow)) {
                mouseY = -1;
            }
            if (Input.GetKey(KeyCode.LeftArrow)) {
                mouseX = -1;
            }
            if (Input.GetKey(KeyCode.RightArrow)) {
                mouseX = 1;
            }

            mouseX *= CAMERA_ROTATION_SPEED * Time.deltaTime;
            mouseY *= CAMERA_ROTATION_SPEED * Time.deltaTime;

            Vector3 verticalRotation = Vector3.up;
            Vector3 horizontalRotation = new Vector3(transform.right.x, 0, transform.right.z);

            transform.rotation = Quaternion.AngleAxis(mouseX, verticalRotation) * transform.rotation;
            transform.rotation = Quaternion.AngleAxis(-mouseY, horizontalRotation) * transform.rotation;
        }
        // lock z rotation
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
    }
}
