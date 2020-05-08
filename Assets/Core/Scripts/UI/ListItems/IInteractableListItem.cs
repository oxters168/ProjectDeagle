public delegate void InteractionEventHandler(object item);

public interface IInteractableListItem
{
    event InteractionEventHandler onClick;
}
