
''' <summary>
''' Represents undo/redo functionality for a single target. By design a the undo/redo methods take a reference to the information they need to perform their functionality.
''' This way a <see cref="System.WeakReference" /> can be held to the target to make memory leaks less likely.
''' </summary>
''' <typeparam name="T"></typeparam>
Public Class SingleTargetUndoRedo(Of T)
	Implements IUndoRedo(Of IChangeInformation(Of T))

#Region " Objects and variables "

	Private m_ThrowWhenNothingToUndo As Boolean
	Private m_ThrowWhenNothingToRedo As Boolean

	''' <summary>
	''' Core implementation of the undo/redo stack. This class is essentially a wrapper for the core implementation with a few extra features.
	''' </summary>
	Private m_InternalUndoRedoStack As CircularUndoRedoStack(Of IChangeInformation(Of T))

	''' <summary>
	''' A weak reference to the target of the undo and redo actions. This reference is passed by design into the IChangeInformation instances to discourage creating hard links between the undo/redo stack and the target.
	''' </summary>
	Private m_TargetReference As WeakReference

	''' <summary>
	''' Flag designed to save a unique identifier of the creating thread.
	''' This identifier is used to make sure that all access to the methods of this class are performed only through the owning thread.
	''' </summary>
	Private ReadOnly m_OwningThreadManagedId As Integer

	''' <summary>
	''' Flag designed to keep track of whether or not an undo or redo operation is in progress.
	''' By design an undo or redo operation should not add any new changes to the undo/redo stack.
	''' This flag allows the code to catch this type of an error and report it to the user.
	''' </summary>
	Private m_IsPerformingAction As Boolean = False

#End Region

#Region " Properties "

	Public ReadOnly Property ThrowWhenNothingToUndo As Boolean
		Get
			Return m_ThrowWhenNothingToUndo
		End Get
	End Property

	Public ReadOnly Property ThrowWhenNothingToRedo As Boolean
		Get
			Return m_ThrowWhenNothingToRedo
		End Get
	End Property

#End Region

#Region " Public methods and functions "

	''' <summary>
	''' Adds a new undoable/redoable operation to the undo/redo stack. If the stack is full the oldest operation on the undo/redo stack is overwritten.
	''' </summary>
	''' <param name="change">Contains the logic needed to undo/redo an operation. It takes a target as its parameter to lower the risk of memory leaks. This parameter can not be null.</param>
	Public Sub PushChange(change As IChangeInformation(Of T)) Implements IUndoRedo(Of IChangeInformation(Of T)).PushChange

		'Make sure that only the thread that owns this instance calls this method.
		VerifyAccessIsOnExpectedThread()

		'Disallow new changes to be added while an undo or redo is in progress since allowing this is likely an error and I can't think of a reason for needing to allow such a thing in an actual application.
		If m_IsPerformingAction Then
			Throw New InvalidOperationException(String.Format("An attempt was made to push a new change to the {0} during either an undo or redo operation. New changes to the undo/redo list are not supported while performing an undo or redo operation.", Me.GetType().FullName))
		End If
		If change Is Nothing Then
			Throw New InvalidOperationException("Attempted to push a null change to the undo/redo stack. Change information can not be null.")
		End If

		m_InternalUndoRedoStack.PushNewChange(change)

	End Sub

	Public Sub Redo() Implements IUndoRedo(Of IChangeInformation(Of T)).Redo

		'Flag that an undo or redo is in progress, this way if a new change is pushed during an undo or redo a meaningful exception can be thrown.
		m_IsPerformingAction = True

		'Make sure that only the thread that owns this instance calls this method.
		VerifyAccessIsOnExpectedThread()

		'Handle the case when redo is called but there is nothing to redo.
		If Not m_InternalUndoRedoStack.CanRedo Then
			If m_ThrowWhenNothingToRedo Then
				Throw New InvalidOperationException("Attempted to redo an operation when there were no more operations to redo.")
			Else
				Return
			End If
		End If

		'Get the change information object that contains the redo logic from the wrapped undo redo stack.
		Dim changeInformation As IChangeInformation(Of T) = m_InternalUndoRedoStack.Redo()
		'Take out a hard reference to the target (the class holds a weak reference by design) and perform the redo.
		Dim target As T = GetTargetReference()
		changeInformation.Redo(target)
		'Reset the flag to indicate that the undo or redo operation has been completed.
		m_IsPerformingAction = False

	End Sub

	Public Sub Undo() Implements IUndoRedo(Of IChangeInformation(Of T)).Undo

		'Flag that an undo or redo is in progress, this way if a new change is pushed during an undo or redo a meaningful exception can be thrown.
		m_IsPerformingAction = True
		'Make sure that only the thread that owns this instance calls this method.
		Me.VerifyAccessIsOnExpectedThread()

		'Handle the case when undo is called but there is nothing to undo.
		If Not m_InternalUndoRedoStack.CanUndo Then
			If m_ThrowWhenNothingToUndo Then
				Throw New InvalidOperationException("Attempted to undo an operation when there were no more operations to undo.")
			Else
				Return
			End If
		End If

		'Get the change information object that contains the undo logic from the wrapped undo redo stack.
		Dim changeInformation As IChangeInformation(Of T) = m_InternalUndoRedoStack.Undo()
		'Take out a hard reference to the target (the class holds a weak reference by design) and perform the undo.
		Dim target As T = GetTargetReference()
		changeInformation.Undo(target)
		'Reset the flag to indicate that the undo or redo operation has been completed.
		m_IsPerformingAction = False

	End Sub

#End Region

#Region " Private methods and functions "

	''' <summary>
	''' Returns a hard reference to the target through the <see cref="System.WeakReference"/> stored in a local field. Throws an exception if the target can no longer be reached (meaning that the target no longer exists in memory). It is still possible though that the target can be reached but has been disposed, there doesn't seem to be a low impact way to check this case however (in this case the target may be use after it is disposed, but in general the programmer is responsible for making sure this doesn't happen anyway).
	''' </summary>
	''' <returns></returns>
	Private Function GetTargetReference() As T

		'Retrieve a hard reference to avoid timing issues from the null check (when the target isn't null at the check but immediately after is changed to null by the garbage collector and then causes an exception).
		Dim target As Object = m_TargetReference.Target
		If target Is Nothing Then
			Throw New ObjectDisposedException(String.Format("The target object of type {0} was disposed so the undo operation can not be performed. The undo/redo stack holds a weak reference to this item so the reference held by the undo/redo stack won't stop the item from being garbage collected.", GetType(T).FullName))
		End If

		Return CType(target, T)

	End Function

	''' <summary>
	''' Verifies that the current thread is the same thread that owns the instance. This allows the code to enforce that access to the instance can only happen through a single thread so that concurrency issues are impossible.
	''' </summary>
	Private Sub VerifyAccessIsOnExpectedThread()
		Dim callingThreadManagedId As Integer = System.Threading.Thread.CurrentThread.ManagedThreadId
		If callingThreadManagedId <> m_OwningThreadManagedId Then
			Throw New ApplicationException(String.Format("Could not perform the specified method call on the {0} instance because the method was called on the wrong thread (access can only occur using the thread that owns the instance). The managed thread id that owns the instance is {1} and the method was called on a thread with managed thread id {2}.", Me.GetType().FullName, m_OwningThreadManagedId.ToString(), callingThreadManagedId.ToString()))
		End If

	End Sub

#End Region

#Region " Construction "

	''' <summary>
	''' Constructor.
	''' </summary>
	''' <param name="target">The instance that will be passed as a parameter to the undo/redo methods. This value can not be null.</param>
	''' <param name="stackSize">The size of the internal undo/redo stack. This number represents the maximum number of undoable actions that can be handled.</param>
	''' <param name="throwsExceptionWhenNothingToUndo">When true causes an exception to be thrown when there are no more operations to undo. When false undo performs no action and throws no exception when there are no more operations to undo.</param>
	''' <param name="throwsExceptionWhenNothingToRedo">When true causes an exception to be thrown when there are no more operations to redo. When false redo performs no action and throws no exception when there are no more operations to redo.</param>
	Public Sub New(target As T, Optional stackSize As Integer = 20, Optional throwsExceptionWhenNothingToUndo As Boolean = False, Optional throwsExceptionWhenNothingToRedo As Boolean = False)

		If target Is Nothing Then
			Throw New ArgumentNullException("target")
		End If
		m_TargetReference = New WeakReference(target, False)
		m_InternalUndoRedoStack = New CircularUndoRedoStack(Of IChangeInformation(Of T))(stackSize)

		m_ThrowWhenNothingToUndo = throwsExceptionWhenNothingToUndo
		m_ThrowWhenNothingToRedo = throwsExceptionWhenNothingToRedo
		' save a reference to the thread that this instance is created on to make sure that all accesses to this class come through that same thread
		m_OwningThreadManagedId = System.Threading.Thread.CurrentThread.ManagedThreadId

	End Sub

#End Region

End Class