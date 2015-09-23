using UnityEngine;
using System.Collections;

public class Cart : MonoBehaviour
{
    public Transform harnessCenter;
    public Transform centerOfMass;

    private Transform m_pony;
    private Transform m_waist;


    // Use this for initialization
    void Start ()
    {
        transform.root.GetComponent<Rigidbody>().centerOfMass = centerOfMass.localPosition;
    }
	
	// Update is called once per frame
	void LateUpdate ()
    {
        if (m_pony && gameObject.GetComponent<ConfigurableJoint>())
        {
            gameObject.GetComponent<ConfigurableJoint>().connectedAnchor = m_pony.InverseTransformPoint(m_waist.position);
        }
	}

    public void Harness(Transform pony)
    {
        m_pony = pony;

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Waist"))
        {
            if (go.transform.root == pony)
            {
                m_waist = go.transform;
            }
        }
        
        Vector3 targetPos = GameController.GetPlayer().position + new Vector3(0, 0.2f, -0.014f);
        Vector3 dir = (targetPos - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dir, GameController.GetPlayer().up);

        if (gameObject.GetComponent<ConfigurableJoint>())
        {
            Destroy(gameObject.GetComponent<ConfigurableJoint>());
        }

        ConfigurableJoint joint;
        joint = gameObject.AddComponent<ConfigurableJoint>();

        joint.autoConfigureConnectedAnchor = false;
        joint.axis = Vector3.up;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Limited;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Limited;
        SoftJointLimit xLimit1 = new SoftJointLimit();
        SoftJointLimit xLimit2 = new SoftJointLimit();
        SoftJointLimit yLimit = new SoftJointLimit();
        SoftJointLimit zLimit = new SoftJointLimit();
        xLimit1.limit = -15.0f;
        xLimit2.limit = 15.0f;
        yLimit.limit = 35.0f;
        zLimit.limit = 35.0f;
        joint.lowAngularXLimit = xLimit1;
        joint.highAngularXLimit = xLimit2;
        joint.angularYLimit = yLimit;
        joint.angularZLimit = zLimit;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.breakForce = float.MaxValue;
        joint.breakTorque = float.MaxValue;

        joint.connectedBody = pony.GetComponent<Rigidbody>();
        joint.anchor = harnessCenter.localPosition;
    }

    public void RemoveHarness()
    {
        m_pony = null;

        if (gameObject.GetComponent<ConfigurableJoint>())
        {
            Destroy(gameObject.GetComponent<ConfigurableJoint>());
        }
    }
}
