
''' <summary>
''' Interface definition for objects that are capable of undo/redo functionality.
''' </summary>
Public Interface IUndoRedo(Of T)

	Sub Undo()
	Sub Redo()
	Sub PushChange(change As T)

End Interface