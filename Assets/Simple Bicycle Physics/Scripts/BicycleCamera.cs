using UnityEngine;

namespace SBPScripts
{
    public class BicycleCamera : MonoBehaviour
    {
        public Transform target;
        public bool stuntCamera;
        public float distance = 20.0f;
        public float height = 5.0f;
        public float heightDamping = 2.0f;
        public float lookAtHeight = 0.0f;
        public float rotationSnapTime = 0.3f;

        private Vector3 lookAtVector;
        private float usedDistance;
        float wantedRotationAngle;
        float wantedHeight;
        float currentRotationAngle;
        float currentHeight;
        Vector3 wantedPosition;

        private float yVelocity = 0.0f;
        private float zVelocity = 0.0f;
        private PerfectMouseLook perfectMouseLook;

        // ƽ���л��߼�
        private Transform currentTarget;
        private bool isSwitching = false;
        private float switchProgress = 0f;
        private float switchDuration = 0.6f; // �л���ʱ

        void Start()
        {
            perfectMouseLook = GetComponent<PerfectMouseLook>();

            if (stuntCamera)
            {
                var follow = new GameObject("Follow");
                var toFollow = GameObject.FindObjectOfType<BicycleController>().transform;
                follow.transform.SetParent(toFollow);
                follow.transform.position = toFollow.position + toFollow.gameObject.GetComponent<BoxCollider>().center;
                target = follow.transform;

                height -= toFollow.gameObject.GetComponent<BoxCollider>().center.y;
                lookAtHeight -= toFollow.gameObject.GetComponent<BoxCollider>().center.y;
            }

            // ��ʼ�� currentTarget
            GameObject temp = new GameObject("CurrentTargetProxy");
            currentTarget = temp.transform;
            currentTarget.position = transform.position;
            //currentTarget.rotation = target.rotation;
        }

        // �ⲿ��������������л�Ŀ��
        public void SetTarget(Transform newTarget)
        {
            if (target != newTarget && newTarget != null)
            {
                target = newTarget;
                switchProgress = 0f;
                isSwitching = true;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // ��ֵ�л�Ŀ��λ��
            if (isSwitching)
            {
                switchProgress += Time.deltaTime / switchDuration;
                float t = Mathf.SmoothStep(0f, 2f, switchProgress); // ƽ����ֵ
                currentTarget.position = Vector3.Lerp(currentTarget.position, target.position, t);
                currentTarget.rotation = Quaternion.Slerp(currentTarget.rotation, target.rotation, t);

                if (switchProgress >= 1f)
                {
                    isSwitching = false;
                }
            }
            else
            {
                // �������� currentTarget Ϊ��ǰ target
                currentTarget.position = target.position;
                currentTarget.rotation = target.rotation;
            }

            wantedHeight = currentTarget.position.y + height;
            currentHeight = transform.position.y;

            wantedRotationAngle = currentTarget.eulerAngles.y;
            currentRotationAngle = transform.eulerAngles.y;

            if (!perfectMouseLook.movement)
            {
                currentRotationAngle = Mathf.SmoothDampAngle(currentRotationAngle, wantedRotationAngle, ref yVelocity, rotationSnapTime);
            }

            currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

            wantedPosition = currentTarget.position;
            wantedPosition.y = currentHeight;

            usedDistance = Mathf.SmoothDampAngle(usedDistance, distance, ref zVelocity, 0.1f);
            wantedPosition += Quaternion.Euler(0, currentRotationAngle, 0) * new Vector3(0, 0, -usedDistance);

            transform.position = wantedPosition;

            lookAtVector = new Vector3(0, lookAtHeight, 0);
            transform.LookAt(currentTarget.position + lookAtVector);
        }
    }
}
