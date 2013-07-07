using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// A class to cluster spatial items in iteratively narrower space boundaries.
    /// Formerly used in XbimMesher to split large models like this:
    /// 
    /// XbimOctree<int> octree = new XbimOctree<int>(bounds.Length(), MaxWorldSize * metre, 1f, bounds.Centroid());
    /// octree.Add(geomData.GeometryLabel, bound);
    /// then iterating over octree.Populated to retrieve the clusters.
    /// 
    /// Warning: If items fall across boundaries of children they stop the iterative split of the parent they fall into.
    /// </summary>
    /// <typeparam name="T">e.g. Int for geometry labels</typeparam>
    public class XbimOctree<T>
    {
        public String Name = "R"; // for Root

        /// <summary>
        /// The number of children in an octree.
        /// </summary>
        private const int ChildCount = 8;

        /// <summary>
        /// The octree's looseness value.
        /// </summary> 
        private float looseness = 0;

        /// <summary>
        /// The octree's depth.
        /// </summary> 
        private int depth = 0;

        /// <summary> 
        /// The octree's centre coordinates.
        /// </summary> 
        private XbimPoint3D centre = XbimPoint3D.Zero;


        /// <summary>
        /// The octree's length.
        /// </summary>
        private float length = 0f;

        ///  <summary>
        /// The bounding box that represents the octree.
        /// </summary> 
        private XbimRect3D bounds = default(XbimRect3D);

        ///  <summary>
        /// The objects in the octree.
        /// </summary> 

        private List<T> objects = new List<T>();

        /// <summary>
        /// The octree's child nodes.
        /// </summary> 
        private XbimOctree<T>[] children = null;

        ///  <summary>
        /// The octree's world size.
        /// </summary> 
        private float worldSize = 0f;
        private float targetCanvasSize = 100000f;

        private XbimRect3D contentBounds = XbimRect3D.Empty;
        
        /// <summary>
        /// Creates a new octree.
        /// </summary>
        /// <param name="worldSize">/// The octree's world size.</param>
        /// <param name="targetCanvasSize">The octree's looseness value.</param>
        /// <param name="looseness">The octree recursion depth.</param>
        public XbimOctree(float worldSize, float targetCanvasSize, float looseness)
            : this(worldSize, targetCanvasSize, looseness, 0, XbimPoint3D.Zero)
        {
        }
        public XbimOctree(float worldSize, float targetCanvasSize, float looseness, XbimPoint3D centre)
            : this(worldSize, targetCanvasSize, looseness, 0, centre)
        {
        }
        

        /// <summary>
        /// Creates a new octree.
        /// </summary>
        /// <param name="worldSize">The octree's world size.</param>
        /// <param name="targetCanvasSize"></param>
        /// <param name="looseness">The octree's looseness value.</param>
        /// <param name="depth">The maximum depth to recurse to.</param>
        /// <param name="centre">The octree's centre coordinates.</param>
        private XbimOctree(float worldSize,float targetCanvasSize, float looseness, int depth, XbimPoint3D centre)
        {
            this.worldSize = worldSize;
            this.targetCanvasSize = targetCanvasSize;
            this.looseness = looseness;
            this.depth = depth;
            this.centre = centre;
            this.length = this.looseness * this.worldSize / (float)Math.Pow(2, this.depth);
            float radius = this.length / 2f;

            // Create the bounding box.
            XbimPoint3D min = this.centre + new XbimVector3D(-radius);
            XbimPoint3D max = this.centre + new XbimVector3D(radius);
            this.bounds = new XbimRect3D(min, max);

            ////// Split the octree if the depth hasn't been reached.
            //if (this.depth < maxDepth)
            //{
            //    this.Split(maxDepth);
            //}
        }

        public XbimOctree(XbimOctree<T> copy)
        {
            worldSize = copy.worldSize;
            length = copy.length;
            objects = copy.objects;
            children = copy.children;
            centre = copy.centre;
            bounds = copy.bounds;
        }

        /// <summary>
        /// Returns the main octrees that are populated
        /// </summary>
        public List<XbimOctree<T>> Populated
        {
            get 
            {   List<XbimOctree<T>> population = new List<XbimOctree<T>>();
                return GetPopulation(population);
            }
        }
        /// <summary>
        /// Returns the total bounds for all contetn under this node
        /// </summary>
        /// <returns></returns>
        public XbimRect3D ContentBounds()
        {
            XbimRect3D b = contentBounds;
            if (children != null)
            {
                foreach (var child in children)
                {
                    XbimRect3D cb = child.ContentBounds();
                    if (!cb.IsEmpty) b.Union(cb);
                }
            }
            return b;
        }

        
        private List<XbimOctree<T>> GetPopulation(List<XbimOctree<T>> population)
        {
            // If anything has been added at any level of the tree all children are ignored
            // this can easily happen if an object added is sitting across children and not completely within.
            // todo: investigate looseness 
            if (this.objects != null && this.objects.Any())
                population.Add(this);
            else if (children != null)
            {
                foreach (var child in children)
                {
                    child.GetPopulation(population);
                }
            }
            return population;
        }
        
        /// <summary>
        /// Removes the specified obj.
        /// </summary>
        /// <param name="obj">the object to remove.</param>
        public void Remove(T obj)
        {
            objects.Remove(obj);
        }

        /// <summary>
        /// Determines whether the specified obj has changed.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="bBox"></param>
        /// <returns>true if the specified obj has changed; otherwise, false.</returns>
        public bool HasChanged(T obj, XbimRect3D bBox)
        {
            return this.bounds.Contains(bBox);
        }

       
        /// <summary>
        /// Adds the given object to the octree.
        /// </summary>
        /// <param name="o">The object to add.</param>
        /// <param name="centre">The object's centre coordinates.</param>
        /// <param name="radius">The object's radius.</param>
        private XbimOctree<T> Add(T o, XbimPoint3D centre, float radius)
        {
            XbimPoint3D min = centre - new XbimVector3D(radius);
            XbimPoint3D max = centre + new XbimVector3D(radius);
            XbimRect3D bounds = new XbimRect3D(min, max);

            if (this.bounds.Contains(bounds))
            {
                return this.Add(o, bounds, centre, radius);
            }
            return null;
        }


        /// <summary>
        /// Adds the given object to the octree.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="bBox"></param>
        public XbimOctree<T> Add(T o, XbimRect3D bBox)
        {
            float radius = bBox.Radius();
            XbimPoint3D centre = bBox.Centroid();
            if (this.bounds.Contains(bBox))
            {
                return this.Add(o, bBox, centre, radius);
            }
            return null;
        }

        /// <summary>
        /// Adds the given object to the octree.
        /// </summary>
        /// <param name="o">The object to add.</param>
        /// <param name="b">The object's bounds.</param>
        /// <param name="centre">The object's centre coordinates.</param>
        /// <param name="radius">The object's radius.</param>
        /// <returns></returns>
        private XbimOctree<T> Add(T o, XbimRect3D b, XbimPoint3D centre, float radius)
        {
            lock (this.objects)
            {
                if (this.children == null && this.bounds.Length() > this.targetCanvasSize)
                    this.Split();
            }
            if (this.children != null)
            {
                // Find which child the object is closest to based on where the
                // object's centre is located in relation to the octree's centre.
                int index = (centre.X <= this.centre.X ? 0 : 1) +
                    (centre.Y >= this.centre.Y ? 0 : 4) +
                    (centre.Z <= this.centre.Z ? 0 : 2);

                // Add the object to the child if it is fully contained within
                // it.
                if (this.children[index].bounds.Contains(b))
                {
                    return this.children[index].Add(o, b, centre, radius);
                }
            }
            // Debug.WriteLine("Addedto: " + this.Name);
            this.objects.Add(o); //otherwise add it to here
            if (contentBounds.IsEmpty) contentBounds = b;
            else contentBounds.Union(b);
            return this;
        }
        /// <summary>
        /// Returns the total content of this octree and all its children
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> ContentIncludingChildContent()
        {
            foreach (var o in objects)
            {
                yield return o;
            }
            if (children != null)
            {
                foreach (var child in children)
                {
                    foreach (var co in child.objects)
                    {
                        yield return co;
                    }
                }
            }
        }
       
        /// <summary>
        /// Splits the octree into eight children.
        /// </summary>
        private void Split()
        {
            this.children = new XbimOctree<T>[XbimOctree<T>.ChildCount];
            int depth = this.depth + 1;
            float quarter = this.length / this.looseness / 4f;

            this.children[0] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(-quarter, quarter, -quarter));
            this.children[1] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(quarter, quarter, -quarter));
            this.children[2] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(-quarter, quarter, quarter));
            this.children[3] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(quarter, quarter, quarter));
            this.children[4] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(-quarter, -quarter, -quarter));
            this.children[5] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(quarter, -quarter, -quarter));
            this.children[6] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(-quarter, -quarter, quarter));
            this.children[7] = new XbimOctree<T>(this.worldSize, this.targetCanvasSize, this.looseness,
                 depth, this.centre + new XbimVector3D(quarter, -quarter, quarter));
            for (int i = 0; i < children.Length; i++)
            {
                children[i].Name = this.Name + i.ToString();
            }
        }
    }
}
