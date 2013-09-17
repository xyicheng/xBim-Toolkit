using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimOctree<T>
    {
         /// 

        /// The number of children in an octree.
        /// 

        private const int ChildCount = 8;

        /// 

        /// The octree's looseness value.
        /// 

        private float looseness = 0;
        /// 

        /// The octree's depth.
        /// 

        private int depth = 0;   
        /// 

        /// The octree's centre coordinates.
        /// 

        private XbimPoint3D centre = XbimPoint3D.Zero;

        /// 

        /// The octree's length.
        /// 

        private float length = 0f;

        /// 
       
        /// The bounding box that represents the octree.
        /// 

        private XbimRect3D bounds = default(XbimRect3D);

        /// 

        /// The objects in the octree.
        /// 

        private List<T> objects = new List<T>();

        /// 

        /// The octree's child nodes.
        /// 

        private XbimOctree<T>[] children = null;

        /// 

        /// The octree's world size.
        /// 

        private float worldSize = 0f;
        private float targetCanvasSize = 100000f;

        private XbimRect3D contentBounds = XbimRect3D.Empty;
        /// 

        /// Creates a new octree.
        /// 
    
        /// The octree's world size.
        /// The octree's looseness value.
        /// The octree recursion depth.
        public XbimOctree(float worldSize, float targetCanvasSize, float looseness)
            : this(worldSize, targetCanvasSize, looseness, 0, XbimPoint3D.Zero)
        {
        }
        public XbimOctree(float worldSize, float targetCanvasSize, float looseness, XbimPoint3D centre)
            : this(worldSize, targetCanvasSize, looseness, 0, centre)
        {
        }
        /// 

        /// Creates a new octree.
        /// 
    
        /// The octree's world size.
        /// The octree's looseness value.
        /// The maximum depth to recurse to.
        /// The octree recursion depth.
        /// The octree's centre coordinates.
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
        /// 

        /// Removes the specified obj.
        /// 

        /// The obj.
        public void Remove(T obj)
        {
            objects.Remove(obj);
        }

        /// 

        /// Determines whether the specified obj has changed.
        /// 

        /// The obj.
        /// The bBox.
        /// 
        ///   true if the specified obj has changed; otherwise, false.
        /// 
        public bool HasChanged(T obj, XbimRect3D bBox)
        {
            return this.bounds.Contains(bBox);
        }

       
        /// 

        /// Adds the given object to the octree.
        /// 

        /// The object to add.
        /// The object's centre coordinates.
        /// The object's radius.
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


        /// 

        /// Adds the given object to the octree.
        /// 

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


        /// 

        /// Adds the given object to the octree.
        /// 

        /// The object to add.
        /// The object's bounds.
        /// The object's centre coordinates.
        /// The object's radius.
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
       
        /// 

        /// Splits the octree into eight children.
        /// 

        /// The maximum depth to recurse to.
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
        }
    }
}
