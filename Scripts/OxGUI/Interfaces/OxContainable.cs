namespace OxGUI
{
    public interface OxContainable
    {
        /// <summary>
        /// Adds items in the parameter into the items list. If an item in the parameter is null, an exception is thrown.
        /// </summary>
        /// <param name="addedItems">Items to be added</param>
        void AddItems(params OxBase[] addedItems);
        /// <summary>
        /// Removes all items in parameters from the items list if they exist within the list.
        /// If an item in the parameters is null, an exception will be thrown.
        /// </summary>
        /// <param name="removedItems">Items to be removed from the items list</param>
        /// <returns>True is return when all items within the parameters that exist within
        /// the items list are removed. If an item within the parameters does not exist in
        /// the items list, it does not change the outcome.</returns>
        bool RemoveItems(params OxBase[] removedItems);
        void RemoveAt(int index);
        void ClearItems();
        /// <summary>
        /// Gets the items within the items list.
        /// </summary>
        /// <returns>A shallow copy of the items</returns>
        OxBase[] GetItems();
        OxBase ItemAt(int index);
        /// <summary>
        /// Gives the index of the item within the items list
        /// </summary>
        /// <param name="item">Item to be indexed</param>
        /// <returns>Index as an integer</returns>
        int IndexOf(OxBase item);
    }
}