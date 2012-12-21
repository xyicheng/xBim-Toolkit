namespace Xbim.SceneJSWebViewer
{
    using System;
    using System.Collections.Generic;
    using Xbim.IO;
    using System.IO;

    /// <summary>
    /// Interface for a model streaming object
    /// </summary>
    public interface IModelStream
    {
        /// <summary>
        /// Initialization function called to perform any housekeeping which may be needed for different modes
        /// </summary>
        /// <param name="model">The id of the model</param>
        void Init(String model);

        /// <summary>
        /// Closes the model stream
        /// </summary>
        void Close();

        /// <summary>
        /// Gets the IFC/Model Types as a list of Strings
        /// </summary>
        /// <returns> A List of Types as Strings</returns>
        List<String> GetTypes();

        /// <summary>
        /// Get's the Data for the Camera settings
        /// </summary>
        /// <returns>A Camera object</returns>
        Camera GetCamera();

        /// <summary>
        /// Performs a query on an item
        /// </summary>
        /// <param name="id">The id of the item to query</param>
        /// <param name="query">The query to perform</param>
        /// <returns>String representation of a JSON object</returns>
        String QueryData(String id, String query);

        /// <summary>
        /// Gets the Materials in the model
        /// </summary>
        /// <returns>A List of Material objects</returns>
        List<XbimSurfaceStyle> GetMaterials();

        /// <summary>
        /// Gets the headers (labels) for the geometry
        /// </summary>
        /// <returns>a list of Geometry Labels</returns>
        List<GeometryHeader> GetGeometryHeaders();

        /// <summary>
        /// Gets the actual geometric data of the geometry
        /// </summary>
        /// <param name="id">the id of the geometry piece</param>
        /// <returns>A GeometryData object</returns>
        MemoryStream GetPNIGeometryData(int id);
    }
}
