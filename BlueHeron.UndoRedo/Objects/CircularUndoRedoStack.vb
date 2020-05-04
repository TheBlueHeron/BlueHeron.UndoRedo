
''' <summary>
''' Special data structure for representing the data needed for undo/redo.
''' It is memory efficient in that it uses only one array for the undo/redo information and that array is not resized during normal use as a regular stack would be.
''' </summary>
''' <typeparam name="T">Type of the object that contains the undo/redo data</typeparam>
Public Class CircularUndoRedoStack(Of T)
	Implements IDisposable

#Region " Objects and variables "

	Private m_Disposed As Boolean
	Private ReadOnly m_MaxSize As Integer
	Private m_Values As T()
	Private m_Index As Integer
	Private m_RedoableChangesCount As Integer
	Private m_TotalUndoableChangesOnStack As Integer

#End Region

#Region " Properties "

	''' <summary>
	''' Determines whether there is a redoable object available.
	''' </summary>
	''' <returns>Boolean</returns>
	Public ReadOnly Property CanRedo As Boolean
		Get
			Return (RedoableChangesCount > 0)
		End Get
	End Property

	''' <summary>
	''' Determines whether there is an undoable object available.
	''' </summary>
	''' <returns>Boolean</returns>
	Public ReadOnly Property CanUndo As Boolean
		Get
			Return (UndoableChangesCount > 0)
		End Get
	End Property

	''' <summary>
	''' Returns the currently available undo object, if present.
	''' </summary>
	''' <returns>An object of type T that holds the current undo data</returns>
	Public ReadOnly Property CurrentUndo As T
		Get
			Return If(CanUndo, m_Values(GetNextReadIndex), Nothing)
		End Get
	End Property

	''' <summary>
	''' Returns the currently available redo object, if present.
	''' </summary>
	''' <returns>An object of type T that holds the current redo data</returns>
	Public ReadOnly Property CurrentRedo As T
		Get
			Return m_Values(m_Index)
		End Get
	End Property

	''' <summary>
	''' Maximum number of undoable changes that are supported.
	''' </summary>
	''' <returns>Integer</returns>
	Public ReadOnly Property MaxSize As Integer
		Get
			Return m_MaxSize
		End Get
	End Property

	''' <summary>
	''' The number of redo operations that can be performed at this time.
	''' </summary>
	''' <returns>Integer</returns>
	Public ReadOnly Property RedoableChangesCount As Integer
		Get
			Return m_RedoableChangesCount
		End Get
	End Property

	''' <summary>
	''' The number of undo operations that can be performed at this time.
	''' </summary>
	''' <returns>Integer</returns>
	Public ReadOnly Property UndoableChangesCount As Integer
		Get
			Return (m_TotalUndoableChangesOnStack - m_RedoableChangesCount)
		End Get
	End Property

	''' <summary>
	''' Returns the array of undo/redo objects that are currently stored.
	''' </summary>
	''' <returns>Array of type T</returns>
	Public ReadOnly Property Values As T()
		Get
			Return m_Values
		End Get
	End Property

#End Region

#Region " Public methods and functions "

	''' <summary>
	''' Clears the collection of undo/redo objects.
	''' </summary>
	Public Sub Clear()

		m_Index = 0
		m_RedoableChangesCount = 0
		m_TotalUndoableChangesOnStack = 0
		ReDim m_Values(m_MaxSize - 1)

	End Sub

	''' <summary>
	''' Adds a new undoable operation to the stack.
	''' </summary>
	''' <param name="value">Object of type T that holds the undo/redo data</param>
	Public Sub PushNewChange(value As T)

		m_Values(m_Index) = value
		m_Index = GetNextWriteIndex()
		'Update the number of undoable changes
		m_TotalUndoableChangesOnStack = Math.Min(m_MaxSize, m_TotalUndoableChangesOnStack + 1)
		'Clear out any redoable changes
		m_RedoableChangesCount = 0

	End Sub

	''' <summary>
	''' Updates the current position within the stack and returns the most recent undoable change.
	''' </summary>
	''' <returns>Object of type T that holds the undo/redo data</returns>
	Public Function Undo() As T
		'Make sure that there is a change to undo
		If Not CanUndo Then
			Throw New InvalidOperationException("Can not undo a change because there are no changes left to undo.")
		End If

		m_Index = GetNextReadIndex()
		m_RedoableChangesCount += 1

		Return m_Values(m_Index)

	End Function

	''' <summary>
	''' Updates the current position within the stack and returns the most recent redoable change.
	''' </summary>
	''' <returns>Object of type T that holds the undo/redo data</returns>
	Public Function Redo() As T

		If Not CanRedo Then
			Throw New InvalidOperationException("Can not redo an undone change since there are no undoable changes remaining.")
		End If

		m_RedoableChangesCount -= 1

		Dim changeToRedo As T = m_Values(m_Index)

		m_Index = GetNextWriteIndex()

		Return changeToRedo

	End Function

#End Region

#Region " Private methods and functions "

	''' <summary>
	''' Returns the current index either for a redo or else for adding a new change when there is an empty number of redos available.
	''' </summary>
	Private Function GetNextWriteIndex() As Integer

		Return ((m_Index + 1) Mod m_MaxSize)

	End Function

	''' <summary>
	''' Returns the index of the next undoable change.
	''' </summary>
	Private Function GetNextReadIndex() As Integer

		Return ((m_Index + m_MaxSize - 1) Mod m_MaxSize)

	End Function

#End Region

#Region " Construction and destruction "

	''' <summary>
	''' Creates a new undo/redo stack.
	''' </summary>
	''' <param name="maxSize">The maximum number of undo operations that will be supported.</param>
	Public Sub New(maxSize As Integer)

		m_MaxSize = Math.Max(1, maxSize)
		ReDim m_Values(m_MaxSize - 1)

	End Sub

	''' <summary>
	''' Cleans up resources.
	''' </summary>
	Protected Overridable Sub Dispose(disposing As Boolean)

		If Not m_Disposed Then
			If disposing Then
				m_Values = Nothing
			End If
		End If
		m_Disposed = True

	End Sub

#End Region

#Region " IDisposable Support "

	Public Sub Dispose() Implements IDisposable.Dispose

		Dispose(True)
		GC.SuppressFinalize(Me)

	End Sub

#End Region

End Class