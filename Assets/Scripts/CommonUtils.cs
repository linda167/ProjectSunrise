using System.Collections;
using UnityEngine;

public static class CommonUtils {
    public static IEnumerator MoveTowardsTargetOverTime(Transform transform, float totalTime, Vector3 targetPosition) {
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / totalTime) {
			transform.position = Vector3.Lerp(transform.position, targetPosition, t);
			yield return null;
        }
    }
}