using UnityEngine;
using TMPro;

namespace DepthVisor.UI
{
    public class CameraOrbitControls : MonoBehaviour
    {
        [Header("Tuning Parameters")]
        [SerializeField] float StartCameraRadius = 10f;
        [SerializeField] float StartCameraElevation = 20f;
        [SerializeField] float StartCameraAzimuth = 0f;
        [SerializeField] float MoveSpeed = 1f;
        [SerializeField] float ZoomSpeed = 1f;
        [SerializeField] float RadiusMin = 5f;
        [SerializeField] float RadiusMax = 25f;

        [Header("Game Objects")]
        [SerializeField] GameObject Target;
        [SerializeField] TextMeshProUGUI RadiusText;
        [SerializeField] TextMeshProUGUI AzimuthText;
        [SerializeField] TextMeshProUGUI ElevationText;

        [System.NonSerialized]
        public bool allowMouseDrag;
        private CameraHemisphere cameraHemisphere;

        public void Start()
        {
            // Initialise the camera hemisphere object at the origin of the target and with the starting radius.
            // Then, move the camera to its starting position using the provided elevation and azimuth. Finally,
            // rotate the camera to point at the target.
            cameraHemisphere = new CameraHemisphere(this, Target.transform.position, StartCameraRadius);
            transform.position = cameraHemisphere.InitialiseCameraPos(Mathf.Deg2Rad * StartCameraElevation, Mathf.Deg2Rad * StartCameraAzimuth);
            transform.LookAt(Target.transform);

            allowMouseDrag = true; // TODO : DO I NEED TO INITIALISE THIS HERE?
        }

        public void LateUpdate()
        {
            float verticalValue = 0.0f;
            float horizontalValue = 0.0f;

            // If mouse drag is disabled by either the mouse being in the options panel or playback control section,
            // or the main mouse button is not being held down, only use the arrow keys for movement
            if (!allowMouseDrag || !Input.GetMouseButton(0))
            {
                verticalValue = Input.GetAxis("Vertical");
                horizontalValue = Input.GetAxis("Horizontal");
            }
            else
            {
                // Otherwise, use the mouse movement to transform the camera position, inverting the directions to
                // give the desired dragging effect
                verticalValue = -Input.GetAxis("Mouse Y");
                horizontalValue = -Input.GetAxis("Mouse X");
            }

            // Get the mouse scroll movement and invert again to give forward for zooming in and backwards for
            // zooming out (TODO : Give option to invert this in options panel??)
            float zoomAmount = -Input.mouseScrollDelta.y;

            // If at least one of the values is not equal to 0, move the camera in the desired direction
            if (verticalValue != 0.0f || horizontalValue != 0.0f || zoomAmount != 0.0f)
            {
                transform.position = cameraHemisphere.MoveCamera(MoveSpeed, verticalValue, horizontalValue);
                transform.position = cameraHemisphere.ZoomCamera(ZoomSpeed, zoomAmount); // TODO : Maybe move to another if statement
                transform.LookAt(Target.transform);
            }
        }

        public void MouseDragEnabled()
        {
            allowMouseDrag = true;
        }

        public void MouseDragDisabled()
        {
            allowMouseDrag = false;
        }

        private class CameraHemisphere
        {
            public static CameraOrbitControls CameraOrbit { get; private set; }
            public Vector3 Origin { get; private set; }
            public float Radius { get; private set; }
            public Vector3 CameraPosition { get; private set; }

            public CameraHemisphere(CameraOrbitControls cameraOrbitControls, Vector3 origin, float radius)
            {
                CameraOrbit = cameraOrbitControls;
                Origin = origin;
                Radius = radius;

                // Set the initial camera position on creation to the edge of the sphere
                // around the target object
                CameraPosition = Origin + new Vector3(0f, 0f, radius);
            }

            public Vector3 InitialiseCameraPos(float elevation, float azimuth)
            {
                // Set the initial elevation and azimuth of the camera. Start by converting its vector
                // position to spherical coordinates, moving it to the global origin for this.
                SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

                // Then add the elevation and azimuth angles (in radians) to the coordinates.
                cameraSpherePos.Elevation += elevation;
                cameraSpherePos.Azimuth += azimuth;

                // DELETE AND ALSO DELETE REFERENCE TO OUTER CLASS THAT IS PASSED IN
                CameraOrbit.AzimuthText.text = "Azimuth: " + (cameraSpherePos.Azimuth * Mathf.Rad2Deg).ToString("0.##") + " deg";
                CameraOrbit.ElevationText.text = "Elevation: " + (cameraSpherePos.Elevation * Mathf.Rad2Deg).ToString("0.##") + " deg";

                // Finally, convert the spherical coordinates back to cartesian, move back to the correct
                // position with regards to the target and store the new position, also returning this vector.
                CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
                return CameraPosition;
            }

            public Vector3 MoveCamera(float moveSpeed, float verticalMove, float horizontalMove)
            {
                // Again, convert cartesian vector to spherical coordinates with target on
                // global origin
                SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

                cameraSpherePos.Elevation += verticalMove * Time.deltaTime * moveSpeed;
                cameraSpherePos.Azimuth += horizontalMove * Time.deltaTime * moveSpeed;

                // DELETE AND ALSO DELETE REFERENCE TO OUTER CLASS THAT IS PASSED IN
                CameraOrbit.AzimuthText.text = "Azimuth: " + (cameraSpherePos.Azimuth * Mathf.Rad2Deg).ToString("0.##") + " deg";
                CameraOrbit.ElevationText.text = "Elevation: " + (cameraSpherePos.Elevation * Mathf.Rad2Deg).ToString("0.##") + " deg";

                // Convert the coordinates back and associate with the true target origin once again.
                // Then, save and return the new vector position.
                CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
                return CameraPosition;
            }

            public Vector3 ZoomCamera(float zoomSpeed, float zoomAmount)
            {
                // Again, convert cartesian vector to spherical coordinates with target on
                // global origin
                SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

                cameraSpherePos.Radius += zoomAmount * Time.deltaTime * zoomSpeed;

                // DELETE AND ALSO DELETE REFERENCE TO OUTER CLASS THAT IS PASSED IN
                CameraOrbit.RadiusText.text = "Radius: " + (cameraSpherePos.Radius).ToString("0.##");

                // Convert the coordinates back and associate with the true target origin once again.
                // Then, save and return the new vector position.
                CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
                return CameraPosition;
            }

            // Formulae based on the associated wikipedia page
            private static SphericalCoordinates CartesianToSpherical(Vector3 cartesian)
            {
                SphericalCoordinates sphericalConversion = new SphericalCoordinates(CameraOrbit, 0, 0, 0)
                {
                    Radius = Mathf.Sqrt((cartesian.x * cartesian.x)
                                        + (cartesian.y * cartesian.y)
                                        + (cartesian.z * cartesian.z)),

                    // Use Atan2 to retain quadrant information
                    Azimuth = Mathf.Atan2(cartesian.z, cartesian.x)
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

            // For debugging
            public override string ToString()
            {
                return "Origin: " + Origin + "\n" +
                       "Radius: " + Radius + "\n" +
                       "Camera Position: " + CameraPosition.ToString() + "\n";
            }
        }

        private class SphericalCoordinates
        {
            public static CameraOrbitControls CameraOrbit { get; private set; } // TODO : May want to remove reference to this
            private float radius;
            private float elevation;
            private float azimuth;

            public SphericalCoordinates(CameraOrbitControls cameraOrbitControls, float radius, float elevation, float azimuth)
            {
                CameraOrbit = cameraOrbitControls;
                Radius = radius;
                Elevation = elevation;
                Azimuth = azimuth;
            }

            public float Radius
            {
                get { return radius; }
                set
                {
                    radius = Mathf.Clamp(value, CameraOrbit.RadiusMin, CameraOrbit.RadiusMax);
                }
            }
            public float Elevation
            {
                get { return elevation; }
                set
                {
                    // Clamp the elevation values between 0 and just under pi/2 (90 deg).
                    // This prevents the angle from ever reaching the very top of the
                    // hemisphere, which causes flickering between azimuth quadrants
                    elevation = Mathf.Clamp(value, 0, Mathf.PI / 2 - Mathf.Deg2Rad / 2);
                }
            }
            public float Azimuth
            {
                get { return azimuth; }
                set
                {
                    // Wrap the azimuth value for continual horizontal rotation
                    // between 0 and 2 * pi (360 deg)
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
}
