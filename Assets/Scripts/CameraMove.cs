using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField]
    private float _mouseSensitivity = 3.0f;

    private float _rotationY;
    private float _rotationX;
    private bool _isMouseFreeze = false;
    private float _minFov = 15f;
    private float _maxFov = 90f;
    private float _fov;

    [SerializeField]
    private Transform _target;

    [SerializeField]
    private float _distanceFromTarget = 3.0f;

    private Vector3 _currentRotation;
    private Vector3 _smoothVelocity = Vector3.zero;

    [SerializeField]
    private float _smoothTime = 0.2f;

    [SerializeField]
    private Vector2 _rotationXMinMax = new Vector2(-40, 40);

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;

        // enable free camera based on user input of space key
        if(!_isMouseFreeze){
            _rotationY += mouseX;
            _rotationX += mouseY;

            // Apply clamping for x rotation 
            _rotationX = Mathf.Clamp(_rotationX, _rotationXMinMax.x, _rotationXMinMax.y);

            Vector3 nextRotation = new Vector3(_rotationX, _rotationY);

            // Apply damping between rotation changes
            _currentRotation = Vector3.SmoothDamp(_currentRotation, nextRotation, ref _smoothVelocity, _smoothTime);
            transform.localEulerAngles = _currentRotation;

            // Substract forward vector of the GameObject to point its forward vector to the target
            transform.position = _target.position - transform.forward * _distanceFromTarget;
        }
        
        // camera zoom
        _fov = Camera.main.fieldOfView;
        _fov += (Input.GetAxis("Mouse ScrollWheel") * _mouseSensitivity) * -1;
        _fov= Mathf.Clamp(_fov, _minFov, _maxFov);
        Camera.main.fieldOfView = _fov;

        // stop the free camera move according to the mouse and allow user to select stuffs.
        if (Input.GetKeyDown("space"))
        {
            _isMouseFreeze = !_isMouseFreeze; 
        }

    }
}
