using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    /// <summary>
    /// Abstraction for virtual cursor functionality.
    /// Allows the cursor system to be mocked for testing or replaced with different implementations.
    /// </summary>
    public interface IVirtualCursor
    {
        /// <summary>
        /// Registers a UIDocument as a valid target for cursor interaction.
        /// The cursor visual will be added to this document's hierarchy.
        /// </summary>
        void RegisterDocument(UIDocument document);
    
        /// <summary>
        /// Removes a document from cursor management. Call when closing panels.
        /// </summary>
        void UnregisterDocument(UIDocument document);
    
        /// <summary>
        /// Sets which document currently receives input events.
        /// Only one document can be active at a time (the top-most UI).
        /// </summary>
        void SetActiveDocument(UIDocument document);
    
        /// <summary>
        /// Enables or disables cursor updates. Use when pausing game or opening modal dialogs.
        /// </summary>
        void SetEnabled(bool enabled);
    }
}