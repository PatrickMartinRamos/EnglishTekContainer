namespace ScienceTek.Grade3.U1L5
{
    using UnityEngine;

    public class VolumeKnobSprite : MonoBehaviour
    {
        public float minAngle = 0f; 
        public float maxAngle = 0f; 

        [Range(0f, 1f)]
        public float volume = 0f;
        public enum VolumeType { Music, SFX }
        public VolumeType volumeType;

        Camera cam;
        float dragOffset;
        public AudioSource audio;

        void Start()
        {
            cam = Camera.main;
            if (audio != null)
            {
                audio.volume = volume;
            }
        }

        void OnMouseDown()
        {
            dragOffset = GetMouseAngle() - transform.eulerAngles.z;
        }

        void OnMouseDrag()
        {
            float angle = GetMouseAngle() - dragOffset;
            angle = ClampToTopArc(angle);

            transform.rotation = Quaternion.Euler(0, 0, angle);

            float arcRange = Mathf.Abs(Mathf.DeltaAngle(minAngle, maxAngle));
            float arcValue = Mathf.Abs(Mathf.DeltaAngle(angle, maxAngle));

            volume = Mathf.Clamp01(arcValue / arcRange);
            audio.volume = volume;
        }

        float GetMouseAngle()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(cam.transform.position.z - transform.position.z);

            Vector3 mouseWorld = cam.ScreenToWorldPoint(mousePos);
            Vector2 dir = mouseWorld - transform.position;

            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        }

        float ClampToTopArc(float angle)
        {
            angle = NormalizeAngle(angle);

            if (angle < minAngle && angle > maxAngle)
            {
                float distMin = Mathf.Abs(angle - minAngle);
                float distMax = Mathf.Abs(angle - maxAngle);
                angle = distMin < distMax ? minAngle : maxAngle;
            }

            return angle;
        }

        float NormalizeAngle(float angle)
        {
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        void SetKnobRotation(float vol)
        {
            float angle = Mathf.Lerp(minAngle, maxAngle, vol);
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

}