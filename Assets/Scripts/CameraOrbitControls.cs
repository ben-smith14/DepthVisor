using UnityEngine;
using TMPro;

public class CameraOrbitControls : MonoBehaviour
{
    [SerializeField] GameObject target;
    [SerializeField] float startCameraElevation = 20f;
    [SerializeField] float startCameraAzimuth = 0f;
    [SerializeField] float cameraRadius = 10f;
    [SerializeField] float moveSpeed = 1f;

    [SerializeField] TextMeshProUGUI azimuthText;
    [SerializeField] TextMeshProUGUI elevationText;

    private CameraHemisphere cameraHemisphere;

    public enum Directions
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    public void Start()
    {
        // Initialise the camera hemisphere object at the origin of the target and with a given radius.
        // Then, move the camera to its starting position using the provided elevation and azimuth. Finally,
        //rotate the camera to point at the target.
        cameraHemisphere = new CameraHemisphere(this, target.transform.position, cameraRadius);
        transform.position = cameraHemisphere.InitialiseCameraPos(Mathf.Deg2Rad * startCameraElevation, Mathf.Deg2Rad * startCameraAzimuth);
        transform.LookAt(target.transform);
    }

    public void LateUpdate()
    {
        // For each type of input, give a specific direction to the method that moves the camera and then
        // set the camera to its new position, again rotating it to face the target.

        // Vertical keys
        if (Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
        {
            transform.position = cameraHemisphere.MoveCamera(moveSpeed, Directions.UP);
            transform.LookAt(target.transform);
        } else if (Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.UpArrow))
        {
            transform.position = cameraHemisphere.MoveCamera(moveSpeed, Directions.DOWN);
            transform.LookAt(target.transform);
        }

        // Horizontal keys
        if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        {
            transform.position = cameraHemisphere.MoveCamera(moveSpeed, Directions.LEFT);
            transform.LookAt(target.transform);
        }
        else if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
        {
            transform.position = cameraHemisphere.MoveCamera(moveSpeed, Directions.RIGHT);
            transform.LookAt(target.transform);
        }
    }

    private class CameraHemisphere
    {
        private CameraOrbitControls cameraOrbit;
        private Vector3 origin;
        private float radius;
        private Vector3 cameraPosition;

        public CameraHemisphere(CameraOrbitControls cameraOrbitControls, Vector3 origin, float radius)
        {
            CameraOrbit = cameraOrbitControls;
            Origin = origin;
            Radius = radius;

            // Set the initial camera position on creation to the edge of the sphere
            // around the target object
            CameraPosition = Origin + new Vector3(0f, 0f, radius);
        }

        public CameraOrbitControls CameraOrbit { get; set; }
        public Vector3 Origin { get; set; }
        public float Radius { get; set; }
        public Vector3 CameraPosition { get; set; }

        public Vector3 InitialiseCameraPos(float elevation, float azimuth)
        {
            // Set the initial elevation and azimuth of the camera. Start by converting its vector
            // position to spherical coordinates, moving it to the global origin for this.
            SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

            // Then add the elevation and azimuth angles (in radians) to the coordinates.
            cameraSpherePos.Elevation += elevation;
            cameraSpherePos.Azimuth += azimuth;

            // DELETE AND ALSO DELETE REFERENCE TO OUTER CLASS THAT IS PASSED IN
            CameraOrbit.azimuthText.text = "Azimuth: " + (cameraSpherePos.Azimuth * Mathf.Rad2Deg).ToString("0.##") + " deg";
            CameraOrbit.elevationText.text = "Elevation: " + (cameraSpherePos.Elevation * Mathf.Rad2Deg).ToString("0.##") + " deg";

            // Finally, convert the spherical coordinates back to cartesian, move back to the correct
            // position with regards to the target and store the new position, also returning this vector.
            CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
            return CameraPosition;
        }

        public Vector3 MoveCamera(float moveSpeed, Directions keyDirection)
        {
            // Again, convert cartesian vector to spherical coordinates with target on
            // global origin
            SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

            // For each type of direction specified, increment the apprpriate spherical
            // coordinate angle in the positive or negative direction using the move speed
            // variable
            switch (keyDirection)
            {
                case Directions.UP:
                    cameraSpherePos.Elevation += moveSpeed * Time.deltaTime;
                    break;
                case Directions.DOWN:
                    cameraSpherePos.Elevation -= moveSpeed * Time.deltaTime;
                    break;
                case Directions.LEFT:
                    cameraSpherePos.Azimuth -= moveSpeed * Time.deltaTime;
                    break;
                case Directions.RIGHT:
                    cameraSpherePos.Azimuth += moveSpeed * Time.deltaTime;
                    break;
            }

            // DELETE AND ALSO DELETE REFERENCE TO OUTER CLASS THAT IS PASSED IN
            CameraOrbit.azimuthText.text = "Azimuth: " + (cameraSpherePos.Azimuth * Mathf.Rad2Deg).ToString("0.##") + " deg";
            CameraOrbit.elevationText.text = "Elevation: " + (cameraSpherePos.Elevation * Mathf.Rad2Deg).ToString("0.##") + " deg";

            // Convert the coordinates back and associate with the true target origin once again.
            // Then, save and return the new vector position.
            CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
            return CameraPosition;
        }

        // Formulae from wikipedia
        private static SphericalCoordinates CartesianToSpherical(Vector3 cartesian)
        {
            SphericalCoordinates sphericalConversion = new SphericalCoordinates(0, 0, 0)
            {
                Radius = Mathf.Sqrt((cartesian.x * cartesian.x)
                                    + (cartesian.y * cartesian.y)
                                    + (cartesian.z * cartesian.z)),

                Azimuth = Mathf.Atan2(cartesian.z, cartesian.x) // Use Atan2 to retain quadrant information
            };
            
            sphericalConversion.Elevation = Mathf.Asin(cartesian.y / sphericalConversion.Radius);

            return sphericalConversion;
        }

        private static Vector3 SphericalToCartesian(SphericalCoordinates spherical)
        {
            Vector3 cartesian = new Vector3(0, 0, 0)
            {
                x = spherical.Radius * Mathf.Cos(spherical.Elevation) * Mathf.Cos(spherical.Azimuth),
                y = spherical.Radius * Mathf.Sin(spherical.Elevation),
                z = spherical.Radius * Mathf.Cos(spherical.Elevation) * Mathf.Sin(spherical.Azimuth)
            };
            return cartesian;
        }

        // For debugging purposes
        public override string ToString()
        {
            return "Origin: " + Origin + "\n" +
                   "Radius: " + Radius + "\n" +
                   "Camera Position: " + CameraPosition.ToString() + "\n";
        }
    }

    private class SphericalCoordinates
    {
        private float elevation;
        private float azimuth;

        public SphericalCoordinates(float radius, float elevation, float azimuth)
        {
            Radius = radius;
            Elevation = elevation;
            Azimuth = azimuth;
        }

        public float Radius { get; set; }
        public float Elevation
        {
            get { return elevation; }
            set
            {
                // Clamp the elevation values between 0 and pi (180 deg).
                elevation = Mathf.Clamp(value, 0, Mathf.PI);
            }
        }
        public float Azimuth
        {
            get { return azimuth; }
            set
            {
                // Wrap the azimuth value for continual horizontal rotation
                // between 0 and 2pi (360 deg).
                azimuth = Mathf.Repeat(value, 2 * Mathf.PI);
            }
        }

        // For debugging purposes
        public override string ToString()
        {
            return "Radius: " + Radius + "\n" +
                   "Elevation: " + Elevation + "\n" +
                   "Azimuth: " + Azimuth + "\n";
        }
    }
}
