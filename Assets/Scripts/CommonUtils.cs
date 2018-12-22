using System.Collections;
using UnityEngine;

public static class CommonUtils {
    public static IEnumerator MoveTowardsTargetOverTime(
        Transform transform,
        float totalTime,
        Vector3 targetPosition,
        System.Action callback = null) {

        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / totalTime) {
			transform.position = Vector3.Lerp(transform.position, targetPosition, t);
			yield return null;
        }

        // Trigger callback after move is done
        if (callback != null) {
            callback();
        }
    }
}