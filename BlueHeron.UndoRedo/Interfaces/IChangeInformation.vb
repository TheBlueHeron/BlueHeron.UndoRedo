
''' <summary>
''' Contains the logic for undoing/redoing an operation. Designed to take a reference to a target so that a hard reference to that target does not need to be created (lowers the chances of memory leaks).
''' </summary>
''' <remarks></remarks>
Public Interface IChangeInformation(Of T)

	Sub Undo(target As T)
	Sub Redo(target As T)

End Interface