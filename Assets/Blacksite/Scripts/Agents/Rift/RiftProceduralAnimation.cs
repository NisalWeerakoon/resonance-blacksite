using System;
using Blacksite.Agents.Movement;
using UnityEngine;

namespace Blacksite.Agents.Rift
{
    /// <summary>
    /// Procedural idle and walking pose for RIFT's rigid, bone-weighted model.
    /// The source FBX has a rig but no authored animation clips.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RiftProceduralAnimation : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float walkCyclesPerSecond = 1.8f;
        [SerializeField, Range(5f, 45f)] private float legSwingDegrees = 24f;
        [SerializeField, Range(2f, 35f)] private float armSwingDegrees = 13f;
        [SerializeField, Min(0.1f)] private float blendSpeed = 5f;

        private WaypointAgentMotor motor;
        private Transform hips;
        private Transform spine;
        private Transform chest;
        private Transform head;
        private Transform thighLeft;
        private Transform thighRight;
        private Transform shinLeft;
        private Transform shinRight;
        private Transform footLeft;
        private Transform footRight;
        private Transform upperArmLeft;
        private Transform upperArmRight;
        private Transform forearmLeft;
        private Transform forearmRight;

        private Quaternion hipsRest;
        private Quaternion spineRest;
        private Quaternion chestRest;
        private Quaternion headRest;
        private Quaternion thighLeftRest;
        private Quaternion thighRightRest;
        private Quaternion shinLeftRest;
        private Quaternion shinRightRest;
        private Quaternion footLeftRest;
        private Quaternion footRightRest;
        private Quaternion upperArmLeftRest;
        private Quaternion upperArmRightRest;
        private Quaternion forearmLeftRest;
        private Quaternion forearmRightRest;

        private float phase;
        private float movementBlend;
        private float hitReactionUntil;
        private float hitReactionSide;
        private bool deathAnimating;
        private float deathStartedAt;
        private float deathFallSide;
        private const float HitReactionDuration = .3f;
        private const float DeathAnimationDuration = 1.15f;

        public void TriggerHitReaction(float side)
        {
            hitReactionSide = Mathf.Sign(side);
            hitReactionUntil = Time.time + HitReactionDuration;
        }

        public void TriggerDeathAnimation(float side)
        {
            deathFallSide = Mathf.Sign(side);
            deathStartedAt = Time.time;
            deathAnimating = true;
        }

        private void Awake()
        {
            motor = GetComponent<WaypointAgentMotor>();
            hips = FindBone("hips");
            spine = FindBone("spine");
            chest = FindBone("chest");
            head = FindBone("head");
            thighLeft = FindBone("thigh.L");
            thighRight = FindBone("thigh.R");
            shinLeft = FindBone("shin.L");
            shinRight = FindBone("shin.R");
            footLeft = FindBone("foot.L");
            footRight = FindBone("foot.R");
            upperArmLeft = FindBone("upper_arm.L");
            upperArmRight = FindBone("upper_arm.R");
            forearmLeft = FindBone("forearm.L");
            forearmRight = FindBone("forearm.R");

            hipsRest = Rest(hips);
            spineRest = Rest(spine);
            chestRest = Rest(chest);
            headRest = Rest(head);
            thighLeftRest = Rest(thighLeft);
            thighRightRest = Rest(thighRight);
            shinLeftRest = Rest(shinLeft);
            shinRightRest = Rest(shinRight);
            footLeftRest = Rest(footLeft);
            footRightRest = Rest(footRight);
            upperArmLeftRest = Rest(upperArmLeft);
            upperArmRightRest = Rest(upperArmRight);
            forearmLeftRest = Rest(forearmLeft);
            forearmRightRest = Rest(forearmRight);
        }

        private void Update()
        {
            if (deathAnimating)
            {
                UpdateDeathPose();
                return;
            }
            bool moving = motor != null && motor.IsMoving;
            movementBlend = Mathf.MoveTowards(movementBlend, moving ? 1f : 0f,
                blendSpeed * Time.deltaTime);
            phase += Time.deltaTime * (moving ? walkCyclesPerSecond * Mathf.PI * 2f : 1.4f);

            float idle = 1f - movementBlend;
            float step = Mathf.Sin(phase) * movementBlend;
            float oppositeStep = -step;
            float idleBreath = Mathf.Sin(phase) * idle;
            float hitProgress = Mathf.Clamp01(1f - (hitReactionUntil - Time.time) / HitReactionDuration);
            float hit = hitReactionUntil > Time.time
                ? (1f - hitProgress) * (1f - hitProgress) : 0f;

            Pose(hips, hipsRest, new Vector3(idleBreath * 0.6f, 0f,
                (step - oppositeStep) * 1.4f));
            Pose(spine, spineRest, new Vector3(-movementBlend * 2f + idleBreath * 0.8f, 0f, 0f));
            Pose(chest, chestRest, new Vector3(-movementBlend * 3f + idleBreath * 1.2f,
                hitReactionSide * hit * 7f, (step - oppositeStep) * 1.2f));
            Pose(head, headRest, new Vector3(idleBreath * 1.5f,
                Mathf.Sin(phase * 0.45f) * 2f * idle - hitReactionSide * hit * 11f, 0f));

            float leftLeg = step * legSwingDegrees;
            float rightLeg = oppositeStep * legSwingDegrees;
            Pose(thighLeft, thighLeftRest, new Vector3(leftLeg, 0f, 0f));
            Pose(thighRight, thighRightRest, new Vector3(rightLeg, 0f, 0f));
            Pose(shinLeft, shinLeftRest,
                new Vector3(Mathf.Max(0f, -step) * 22f * movementBlend, 0f, 0f));
            Pose(shinRight, shinRightRest,
                new Vector3(Mathf.Max(0f, step) * 22f * movementBlend, 0f, 0f));
            Pose(footLeft, footLeftRest, new Vector3(-leftLeg * 0.3f, 0f, 0f));
            Pose(footRight, footRightRest, new Vector3(-rightLeg * 0.3f, 0f, 0f));

            Pose(upperArmLeft, upperArmLeftRest,
                new Vector3(-step * armSwingDegrees + idleBreath * 1.5f + hit * 15f,
                    0f, hitReactionSide * hit * 16f));
            Pose(upperArmRight, upperArmRightRest,
                new Vector3(-oppositeStep * armSwingDegrees - idleBreath * 1.5f + hit * 12f,
                    0f, -hitReactionSide * hit * 18f));
            Pose(forearmLeft, forearmLeftRest,
                new Vector3(Mathf.Max(0f, -step) * 8f * movementBlend + hit * 18f, 0f, 0f));
            Pose(forearmRight, forearmRightRest,
                new Vector3(Mathf.Max(0f, step) * 8f * movementBlend + hit * 14f, 0f, 0f));
        }

        private void UpdateDeathPose()
        {
            float progress = Mathf.Clamp01((Time.time - deathStartedAt) / DeathAnimationDuration);
            float brace = 1f - SmoothSegment(progress, 0f, .25f);
            float fall = SmoothSegment(progress, .08f, .72f);
            float settle = SmoothSegment(progress, .55f, 1f);
            float flail = Mathf.Sin(progress * Mathf.PI * 12f) * (1f - fall) * 8f;

            Pose(hips, hipsRest, new Vector3(-5f * fall, deathFallSide * 5f * fall, deathFallSide * 8f * fall));
            Pose(spine, spineRest, new Vector3(8f * fall, 0f, deathFallSide * 5f * fall));
            Pose(chest, chestRest, new Vector3(12f * fall + 5f * brace,
                deathFallSide * 8f * fall, deathFallSide * (8f * fall + flail * .4f)));
            Pose(head, headRest, new Vector3(-8f * fall,
                -deathFallSide * 12f * fall, -deathFallSide * 4f * fall));

            Pose(thighLeft, thighLeftRest, new Vector3(-10f * fall, 0f, -deathFallSide * (12f + 4f * fall)));
            Pose(thighRight, thighRightRest, new Vector3(18f * fall, 0f, -deathFallSide * (7f + 3f * fall)));
            Pose(shinLeft, shinLeftRest, new Vector3(38f * fall, 0f, 0f));
            Pose(shinRight, shinRightRest, new Vector3(52f * fall, 0f, 0f));
            Pose(footLeft, footLeftRest, new Vector3(-18f * fall, 0f, 0f));
            Pose(footRight, footRightRest, new Vector3(-22f * fall, 0f, 0f));

            Pose(upperArmLeft, upperArmLeftRest,
                new Vector3(-20f * fall + flail, 0f, deathFallSide * (20f + 15f * fall)));
            Pose(upperArmRight, upperArmRightRest,
                new Vector3(-12f * fall - flail, 0f, -deathFallSide * (18f + 12f * fall)));
            Pose(forearmLeft, forearmLeftRest, new Vector3(-42f * fall * (1f - settle * .15f), 0f, 0f));
            Pose(forearmRight, forearmRightRest, new Vector3(-35f * fall * (1f - settle * .15f), 0f, 0f));
        }

        private Transform FindBone(string boneName)
        {
            foreach (var candidate in GetComponentsInChildren<Transform>(true))
                if (string.Equals(candidate.name, boneName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            return null;
        }

        private static float SmoothSegment(float value, float start, float end)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, value));
        }

        private static Quaternion Rest(Transform bone) => bone != null ? bone.localRotation : Quaternion.identity;

        private static void Pose(Transform bone, Quaternion rest, Vector3 eulerOffset)
        {
            if (bone != null)
                bone.localRotation = rest * Quaternion.Euler(eulerOffset);
        }
    }
}
