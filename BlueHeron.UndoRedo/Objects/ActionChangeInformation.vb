
''' <summary>
''' Simple implementation of <see cref="IChangeInformation"/> that provides an easy way for implementing undo/redo functionality.
''' </summary>
''' <remarks></remarks>
Public Class ActionChangeInformation(Of T)
	Implements IChangeInformation(Of T)

#Region " Objects and variables "

	Private ReadOnly m_UndoAction As Action(Of T)
	Private ReadOnly m_RedoAction As Action(Of T)

#End Region

#Region " Public methods and functions "

	Public Sub Redo(target As T) Implements IChangeInformation(Of T).Redo

		m_RedoAction(target)

	End Sub

	Public Sub Undo(target As T) Implements IChangeInformation(Of T).Undo

		m_UndoAction(target)

	End Sub

#End Region

#Region " Construction "

	Public Sub New(undoAction As Action(Of T), redoAction As Action(Of T))

		If (undoAction Is Nothing) Or (redoAction Is Nothing) Then
			Throw New ArgumentNullException
		End If
		m_UndoAction = undoAction
		m_RedoAction = redoAction

	End Sub

#End Region

End Class