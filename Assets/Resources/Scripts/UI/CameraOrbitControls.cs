using UnityEngine;

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

        [Header("Target Object")]
        [SerializeField] GameObject MeshTargetContainer = null;

        private CameraHemisphere cameraHemisphere;
        private bool allowMouseMovement;

        void Start()
        {
            // Initialise the camera hemisphere object at the centre of the target mesh container and with the
            // starting radius. Then, move the camera to its starting position using the provided elevation and
            // azimuth. Finally, rotate the camera to point at the target
            cameraHemisphere = new CameraHemisphere(this, MeshTargetContainer.transform.position, StartCameraRadius);
            transform.position = cameraHemisphere.InitialiseCameraPos(Mathf.Deg2Rad * StartCameraElevation, Mathf.Deg2Rad * StartCameraAzimuth);
            transform.LookAt(MeshTargetContainer.transform);

            // Allow mouse drag to rotate the camera initially
            allowMouseMovement = true;
        }

        void LateUpdate()
        {
            float verticalValue = 0.0f;
            float horizontalValue = 0.0f;

            // If mouse drag is disabled because the mouse is over some core UI controls or the main mouse
            // button is not being held down, only use the arrow keys for movement
            if (!allowMouseMovement || !Input.GetMouseButton(0))
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

            // Get the mouse scroll movement and invert again to set forward scrolling for zooming in and backwards
            // scrolling for zooming out (Remove minus sign if scrolling is inverted on a given system)
            float zoomAmount = -Input.mouseScrollDelta.y;

            // If at least one of the movement values is not equal to 0, move the camera in the desired direction
            if (verticalValue != 0.0f || horizontalValue != 0.0f)
            {
                transform.position = cameraHemisphere.MoveCamera(MoveSpeed, verticalValue, horizontalValue);
                transform.LookAt(MeshTargetContainer.transform);
            }

            // If mouse movement is allowed and the zoom amount is not equal to zero, also adjust the camera radius
            if (allowMouseMovement && zoomAmount != 0.0f)
            {
                transform.position = cameraHemisphere.ZoomCamera(ZoomSpeed, zoomAmount);
                transform.LookAt(MeshTargetContainer.transform);
            }
        }

        // Exposed methods to be used as event triggers for when the mouse is entering or
        // exiting core UI controls
        public void MouseMovementEnabled()
        {
            allowMouseMovement = true;
        }

        public void MouseMovementDisabled()
        {
            allowMouseMovement = false;
        }

        // The class that represents the camera hemisphere and is responsible for changing and storing
        // the camera's current position on that hemisphere
        private class CameraHemisphere
        {
            public static CameraOrbitControls CameraOrbit { get; private set; }

            public Vector3 Origin { get; private set; }
            public float Radius { get; private set; }
            public Vector3 CameraPosition { get; private set; }

            public CameraHemisphere(CameraOrbitControls cameraOrbitControls, Vector3 origin, float radius)
            {
                // Store a static reference to the outer parent class if it doesn't already exist, as nested
                // classes in C# cannot access the member variables of their outer parent class otherwise
                if (CameraOrbit == null)
                {
                    CameraOrbit = cameraOrbitControls;
                }

                Origin = origin;
                Radius = radius;

                // Set the initial camera position on creation of the hemisphere to its edge
                CameraPosition = Origin + new Vector3(0f, 0f, radius);
            }

            public Vector3 InitialiseCameraPos(float elevation, float azimuth)
            {
                // Set the initial elevation and azimuth of the camera. Start by converting its vector
                // position to spherical coordinates, moving it to the global origin for this
                SphericalCoordinates cameraSpherePos = CartesianToSpherical(CameraPosition - Origin);

                // Then add the elevation and azimuth angles (in radians) to the coordinates.
                cameraSpherePos.Elevation += elevation;
                cameraSpherePos.Azimuth += azimuth;

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

                // Add the vertical and horizontal movement amounts to the relevant angles, adjusting
                // them using the serialized move speed and delta time
                cameraSpherePos.Elevation += verticalMove * Time.deltaTime * moveSpeed;
                cameraSpherePos.Azimuth += horizontalMove * Time.deltaTime * moveSpeed;

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

                // Add the zoom movement to the radius, again adjusting it using the zoom speed
                // and delta time
                cameraSpherePos.Radius += zoomAmount * Time.deltaTime * zoomSpeed;

                // Convert the coordinates back and associate with the true target origin once again.
                // Then, save and return the new vector position.
                CameraPosition = Origin + SphericalToCartesian(cameraSpherePos);
                return CameraPosition;
            }

            // Formulae for converting between cartesian and spherical coordinate systems, adjusted
            // for the Unity world space coordinate frame
            private static SphericalCoordinates CartesianToSpherical(Vector3 cartesian)
            {
                SphericalCoordinates sphericalConversion = 
                    new SphericalCoordinates(0, 0, 0, CameraOrbit.RadiusMin, CameraOrbit.RadiusMax)
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

            // Formulae for converting back to cartesian coordinates from spherical, again adjusted for
            // the Unity world space coordinate frame
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

            // The class that acts as a store for spherical coordinates in a similar way
            // to the Vector3 class for cartesian coordinates
            private class SphericalCoordinates
            {
                private float radius;
                private float elevation;
                private float azimuth;

                private float RadiusMin { get; set; }
                private float RadiusMax { get; set; }

                public SphericalCoordinates(float radius, float elevation, float azimuth, float radiusMin, float radiusMax)
                {
                    Radius = radius;
                    Elevation = elevation;
                    Azimuth = azimuth;
                    RadiusMin = radiusMin;
                    RadiusMax = radiusMax;
                }

                public float Radius
                {
                    get { return radius; }
                    set
                    {
                        // Clamp the radius between the input min and max values
                        radius = Mathf.Clamp(value, RadiusMin, RadiusMax);
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
            }
        }
    }
}
