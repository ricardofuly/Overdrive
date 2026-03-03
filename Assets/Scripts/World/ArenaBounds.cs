using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    [SerializeField] private GameObject wallPrefab;

    private GameObject leftWall;
    private GameObject rightWall;

    public void CreateBounds(Camera cam)
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        Vector3 center = cam.transform.position;

        float thickness = 1f;

        // Left wall
        leftWall = Instantiate(wallPrefab);
        leftWall.transform.position = center + Vector3.left * (width / 2f + thickness / 2f);
        leftWall.transform.localScale = new Vector3(thickness, height, 10f);

        // Right wall
        rightWall = Instantiate(wallPrefab);
        rightWall.transform.position = center + Vector3.right * (width / 2f + thickness / 2f);
        rightWall.transform.localScale = new Vector3(thickness, height, 10f);
    }

    public void RemoveBounds()
    {
        if (leftWall) Destroy(leftWall);
        if (rightWall) Destroy(rightWall);
    }
}
