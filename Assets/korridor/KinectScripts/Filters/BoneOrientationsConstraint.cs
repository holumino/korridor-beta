//------------------------------------------------------------------------------
// <copyright file="BoneOrientationConstraints.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Filter to correct the joint locations and joint orientations to constraint to range of viable human motion.
/// </summary>
public class BoneOrientationsConstraint
{
    // The Joint Constraints.  
    private readonly List<BoneOrientationConstraint> jointConstraints = new List<BoneOrientationConstraint>();

    // Set true if the bone constraints are mirrored.  
    private bool constraintsMirrored;


    /// Initializes a new instance of the BoneOrientationConstraints class.
    public BoneOrientationsConstraint()
    {
    }

    /// <summary>
    /// AddJointConstraint - Adds a joint constraint to the system.  
    /// </summary>
    /// <param name="joint">The skeleton joint/bone.</param>
    /// <param name="dir">The absolute dir for the center of the constraint cone.</param>
    /// <param name="angle">The angle of the constraint cone that the bone can move in.</param>
    public void AddBoneOrientationConstraint(int joint, Vector3 dir, float angle)
    {
        this.constraintsMirrored = false;
        BoneOrientationConstraint jc = new BoneOrientationConstraint(joint, dir, angle);
        this.jointConstraints.Add(jc);
    }

    // AddDefaultConstraints - Adds a set of default joint constraints for normal human poses.  
    // This is a reasonable set of constraints for plausible human bio-mechanics.
    public void AddDefaultConstraints()
    {
        // Constraints indexed on end joint (i.e. constrains the bone between start and end), but constraint applies at the start joint (i.e. the parent)
        // The constraint is defined in the local coordinate system of the parent bone, relative to the parent bone direction 

        // Acts at Hip Center (constrains hip center to spine bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.Spine, new Vector3(0.0f, 1.0f, 0.3f), 90.0f);

        // Acts at Spine (constrains spine to shoulder center bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderCenter, new Vector3(0.0f, 1.0f, 0.0f), 50.0f);

        // Acts at Shoulder Center (constrains shoulder center to head bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.Head, new Vector3(0.0f, 1.0f, 0.3f), 45.0f);

        // Acts at Shoulder Joint (constraints shoulder-elbow bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft, new Vector3(0.1f, 0.7f, 0.7f), 80.0f);   // along the bone, (i.e +Y), and forward +Z, enable 80 degrees rotation away from this
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.ElbowRight, new Vector3(-0.1f, 0.7f, 0.7f), 80.0f);

        // Acts at Elbow Joint (constraints elbow-wrist bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.WristLeft, new Vector3(0.0f, 0.0f, 1.0f), 90.0f);   // +Z (i.e. so rotates up or down with twist, when arm bent, stop bending backwards)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.WristRight, new Vector3(0.0f, 0.0f, 1.0f), 90.0f);

        // Acts at Wrist Joint (constrains wrist-hand bone)
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft, new Vector3(0.0f, 1.0f, 0.0f), 45.0f);    // +Y is along the bone
        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.HandRight, new Vector3(0.0f, 1.0f, 0.0f), 45.0f);

//        // Acts at Hip Joint (constrains hip-knee bone)
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft, new Vector3(0.5f, 0.7f, 0.1f), 65.0f);
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight, new Vector3(-0.5f, 0.7f, 0.1f), 65.0f);
//
//        // Acts at Knee Joint (constrains knee-ankle bone)
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight, new Vector3(0.0f, 0.7f, -1.0f), 65.0f); // enable bending backwards with -Z
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft, new Vector3(0.0f, 0.7f, -1.0f), 65.0f);
//
//        // Acts at Ankle Joint (constrains ankle-foot bone)
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.FootRight, new Vector3(0.0f, 0.3f, 0.5f), 40.0f);
//        this.AddBoneOrientationConstraint((int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft, new Vector3(0.0f, 0.3f, 0.5f), 40.0f);
    }

    // ApplyBoneOrientationConstraints and constrain rotations.
    public void Constrain(ref KinectWrapper.NuiSkeletonData skeleton, ref Matrix4x4[] jointOrientations, bool mirrorView)
    {
        if (/**null == skeleton ||*/ skeleton.eTrackingState != KinectWrapper.NuiSkeletonTrackingState.SkeletonTracked)
        {
            return;
        }

        if (this.jointConstraints.Count == 0)
        {
            AddDefaultConstraints();
        }

        if (mirrorView != this.constraintsMirrored)
        {
            MirrorConstraints();
        }

        // Constraints are defined as a vector with respect to the parent bone vector, and a constraint angle, 
        // which is the maximum angle with respect to the constraint axis that the bone can move through.

        // Calculate constraint values. 0.0-1.0 means the bone is within the constraint cone. Greater than 1.0 means 
        // it lies outside the constraint cone.
        for (int i = 0; i < this.jointConstraints.Count; i++)
        {
            BoneOrientationConstraint jc = this.jointConstraints[i];

            if (skeleton.eSkeletonPositionTrackingState[jc.Joint] == KinectWrapper.NuiSkeletonPositionTrackingState.NotTracked || 
				jc.Joint == (int)KinectWrapper.NuiSkeletonPositionIndex.HipCenter) 
            {
                // End joint is not tracked or Hip Center has no parent to perform this calculation with.
                continue;
            }

            // If the joint has a parent, constrain the bone direction to be within the constraint cone
            int parentIdx = KinectWrapper.GetSkeletonJointParent(jc.Joint);

            // Local bone orientation relative to parent
            Matrix4x4 boneOrientationRelativeToParent = jointOrientations[parentIdx].inverse * jointOrientations[jc.Joint];
			Vector3 boneOrientationRelativeToParentFwd = (Vector3)boneOrientationRelativeToParent.GetColumn(2);
			if(boneOrientationRelativeToParentFwd == Vector3.zero)
				continue;
			
            Quaternion boneOrientationRelativeToParentQuat = Quaternion.LookRotation(boneOrientationRelativeToParentFwd, boneOrientationRelativeToParent.GetColumn(1));

            // Local bone direction is +Y vector in parent coordinate system
            Vector3 boneRelDirVecLs = (Vector3)boneOrientationRelativeToParent.GetColumn(1);
            boneRelDirVecLs.Normalize();

            // Constraint is relative to the parent orientation, which is +Y/identity relative rotation
            Vector3 constraintDirLs = jc.Dir;
            constraintDirLs.Normalize();

            // Test this against the vector of the bone to find angle
            float boneDotConstraint = Vector3.Dot(boneRelDirVecLs, constraintDirLs);

            // Calculate the constraint value. 0.0 is in the center of the constraint cone, 1.0 and above are outside the cone.
            jc.Constraint = (float)Mathf.Acos(boneDotConstraint) / (Mathf.Deg2Rad * jc.Angle);

            this.jointConstraints[i] = jc;

            // Slerp between identity and the inverse of the current constraint rotation by the amount over the constraint amount
            if (jc.Constraint > 1)
            {
                Quaternion inverseRotation = Quaternion.Inverse(boneOrientationRelativeToParentQuat);
                Quaternion slerpedInverseRotation = Quaternion.Slerp(Quaternion.identity, inverseRotation, jc.Constraint - 1);
                Quaternion constrainedRotation = boneOrientationRelativeToParentQuat * slerpedInverseRotation;

                // Put it back into the bone orientations
				boneOrientationRelativeToParent.SetTRS(Vector3.zero, constrainedRotation, Vector3.one); 
				jointOrientations[jc.Joint] = jointOrientations[parentIdx] * boneOrientationRelativeToParent;
            }
        }
    }

    /// <summary>
    /// Helper method to swap mirror the skeleton bone constraints when the skeleton is mirrored.
    /// </summary>
    private void MirrorConstraints()
    {
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.ElbowLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.ElbowRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.WristLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.WristRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.HandLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.HandRight);

        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.HipLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.HipRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.KneeLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.KneeRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.AnkleLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.AnkleRight);
        this.SwapJointTypes((int)KinectWrapper.NuiSkeletonPositionIndex.FootLeft, (int)KinectWrapper.NuiSkeletonPositionIndex.FootRight);

        for (int i = 0; i < this.jointConstraints.Count; i++)
        {
            BoneOrientationConstraint jc = this.jointConstraints[i];

            // Here we negate the X axis to change the skeleton to mirror the user's movements.
            jc.Dir.x = -jc.Dir.x;

            this.jointConstraints[i] = jc;
        }

        this.constraintsMirrored = !this.constraintsMirrored;
    }

    // Helper method to swap two joint types in the skeleton when mirroring the avatar.
    private void SwapJointTypes(int left, int right)
    {
        for (int i = 0; i < this.jointConstraints.Count; i++)
        {
            BoneOrientationConstraint jc = this.jointConstraints[i];

            if (jc.Joint == left)
            {
                jc.Joint = right;
            }
            else if (jc.Joint == right)
            {
                jc.Joint = left;
            }

            this.jointConstraints[i] = jc;
        }
    }

    // Joint Constraint structure to hold the constraint axis, angle and cone direction and associated joint.
    private struct BoneOrientationConstraint
    {
        /// <summary>
        /// Constraint cone direction
        /// </summary>
        public Vector3 Dir;

        /// <summary>
        /// Skeleton joint
        /// </summary>
        public int Joint;

        /// <summary>
        /// Constraint cone angle
        /// </summary>
        public float Angle;

        /// <summary>
        /// Calculated dynamic value of constraint
        /// </summary>
        public float Constraint;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoneOrientationConstraint"/> struct.
        /// </summary>
        /// <param name="joint">The joint/bone the constraint refers to.</param>
        /// <param name="dir">The constraint cone center direction.</param>
        /// <param name="angle">The constraint cone angle from the center direction.</param>
        public BoneOrientationConstraint(int joint, Vector3 dir, float angle)
        {
            this.Joint = joint;
            this.Dir = dir;
            this.Angle = angle;
            this.Constraint = 0;
        }
    }
}