// Work items:
// 1) The attribute change events on the links are firing too late to find the parent. Switch to remove on the NamedElement itself. (not working)
// 2) RolePlayerChanged needs to be handled
// 3) Check earlier if the element has a string or not. We go a very long way before bailing for lack of a name.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Modeling;
namespace Northface.Tools.ORM.ObjectModel
{
	#region LocatedElement structure
	/// <summary>
	/// A structure to return a located element, or a collection
	/// of elements with the same name.
	/// </summary>
	[CLSCompliant(true)]
	public struct LocatedElement
	{
		/// <summary>
		/// An empty LocatedElement structure
		/// </summary>
		public static LocatedElement Empty = new LocatedElement();
		private object myElement;
		/// <summary>
		/// Construct with a single element
		/// </summary>
		/// <param name="singleElement">A NamedElement object</param>
		public LocatedElement(NamedElement singleElement)
		{
			myElement = singleElement;
		}
		/// <summary>
		/// Construct with multiple elements
		/// </summary>
		/// <param name="multipleElements">A collection of NamedElement objects</param>
		public LocatedElement(ICollection multipleElements)
		{
			myElement = multipleElements;
		}
		/// <summary>
		/// Construct with single or multiple elements
		/// </summary>
		/// <param name="element">A NamedElement or a collection of NamedElements</param>
		public LocatedElement(object element)
		{
			Debug.Assert(element is NamedElement || element is ICollection);
			myElement = element;
		}
		/// <summary>
		/// Get a single element. Returns null
		/// if multiple elements were found with the same name.
		/// </summary>
		public NamedElement SingleElement
		{
			get
			{
				return myElement as NamedElement;
			}
		}
		/// <summary>
		/// Get a collection of elements. Returns null if only a
		/// single element was found.
		/// </summary>
		public ICollection MultipleElements
		{
			get
			{
				return myElement as ICollection;
			}
		}
		/// <summary>
		/// Is the element empty?
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return myElement == null;
			}
		}
	}
	#endregion // LocatedElement structure
	#region IDuplicateNameCollectionManager interface
	/// <summary>
	/// A callback interface to construct a collection of
	/// elements with the same name. A concrete implementation
	/// can either collection the elements into an array-like
	/// collection, or create IMS objects for tracking the errors
	/// in the object model.
	/// </summary>
	public interface IDuplicateNameCollectionManager
	{
		/// <summary>
		/// An element was added whose name conflicted with one or
		/// more existing elements. This method should create or add
		/// to a collection of these elements. OnDuplicateElementAdded
		/// will be called twice when the first duplicate is found. The
		/// first call will have elementCollection == null and should be
		/// used to create the collection and add the first item. The second
		/// call will return the created collection with the second element.
		/// </summary>
		/// <param name="elementCollection">An existing collection, or null
		/// to initialize the collection and populate it with the first element.</param>
		/// <param name="element">The duplicate element</param>
		/// <param name="afterTransaction">The store is not in a transaction, so store
		/// modifications should not be made. This parameter will be true during
		/// events, which only fire during undo/redo. In this state, a collection that
		/// is implemented through the IMS will be extant in the desired state in
		/// the store and needs to be located and returned.</param>
		/// <returns>A new (or modified) collection containing all elements.</returns>
		ICollection OnDuplicateElementAdded(ICollection elementCollection, NamedElement element, bool afterTransaction);
		/// <summary>
		/// A duplicate element has been removed. This method is also responsible
		/// for destroying the collection as the last element is removed.
		/// </summary>
		/// <param name="elementCollection">An existing collection containing the
		/// removed element and other duplicates.</param>
		/// <param name="element">The element to remove.</param>
		/// <param name="afterTransaction">The store is not in a transaction, so store
		/// modifications should not be made. This parameter will be true during
		/// events, which only interaction with the name dictionaries during undo/redo.
		/// In this state, a collection implemented through the IMS should not be modified.</param>
		/// <returns>A new (or modified) collection containing all elements.</returns>
		ICollection OnDuplicateElementRemoved(ICollection elementCollection, NamedElement element, bool afterTransaction);
	}
	#endregion // IDuplicateNameCollectionManager
	#region DuplicateNameAction enum
	/// <summary>
	/// Specifies how like-named elements should be
	/// handled by the INamedElementDictionary implementation.
	/// </summary>
	[CLSCompliant(true)]
	public enum DuplicateNameAction
	{
		/// <summary>
		/// Create or modify a duplicate element collection when a duplicate
		/// is added or removed. Maps to afterTransaction=true parameter values
		/// in calls to the IDuplicateNameCollectionManager interface.
		/// </summary>
		ModifyDuplicateCollection,
		/// <summary>
		/// Retrieve a duplicate element collection when a duplicate
		/// is added or removed. Maps to afterTransaction=false parameter values
		/// in calls to the IDuplicateNameCollectionManager interface.
		/// </summary>
		RetrieveDuplicateCollection,
		/// <summary>
		/// Disallow a new duplicate.
		/// </summary>
		ThrowOnDuplicateName,
	}
	#endregion // DuplicateNameAction enum
	#region INamedElementDictionary interface
	/// <summary>
	/// An interface used to manage names on an object
	/// and provide a quick lookup mechanism for retrieving them.
	/// </summary>
	[CLSCompliant(true)]
	public interface INamedElementDictionary
	{
		/// <summary>
		/// An element has been added. Generate a unique name
		/// if the name is empty, or ensure a unique name if
		/// the Name property is not set.
		/// </summary>
		/// <param name="element">The element to add.</param>
		/// <param name="duplicateAction">Specify the action
		/// to take if the name is already in use in the dictionary.</param>
		void AddElement(NamedElement element, DuplicateNameAction duplicateAction);
		/// <summary>
		/// An element is being removed.
		/// </summary>
		/// <param name="element">The element to remove</param>
		/// <param name="alternateElementName">If specified, a name to use instead of
		/// the current element name value</param>
		/// <param name="duplicateAction">Specify the action
		/// to take if the name is already in use in the dictionary.</param>
		void RemoveElement(NamedElement element, string alternateElementName, DuplicateNameAction duplicateAction);
		/// <summary>
		/// An element is being replaced with another element.
		/// </summary>
		/// <param name="originalElement">The element to remove</param>
		/// <param name="replacementElement">The element to replace it with</param>
		/// <param name="duplicateAction">Specify the action
		/// to take if the name is already in use in the dictionary.</param>
		void ReplaceElement(NamedElement originalElement, NamedElement replacementElement, DuplicateNameAction duplicateAction);
		/// <summary>
		/// An element has been renamed.
		/// </summary>
		/// <param name="element">The element being renamed</param>
		/// <param name="oldName">The old name for the element</param>
		/// <param name="newName">The new name for the element</param>
		/// <param name="duplicateAction">Specify the action
		/// to take if the name is already in use in the dictionary.</param>
		void RenameElement(NamedElement element, string oldName, string newName, DuplicateNameAction duplicateAction);
		/// <summary>
		/// Find elements matching the given name 
		/// </summary>
		/// <param name="elementName">The name to locate</param>
		/// <returns>A LocatedElement structure, indicating no, 1, or
		/// multiple matches.</returns>
		LocatedElement GetElement(string elementName);
	}
	#endregion // INamedElementDictionary interface
	#region INamedElementDictionaryParent interface
	/// <summary>
	/// An interface implemented on the parent role player
	/// in a name dictionary setup.
	/// </summary>
	[CLSCompliant(true)]
	public interface INamedElementDictionaryParent
	{
		/// <summary>
		/// Get the dictionary corresponding to the elements
		/// at the counterpart end of the relationship.
		/// </summary>
		/// <param name="parentMetaRoleGuid">The role played by
		/// the implementing object. Generally, this will
		/// be called for an aggregating role.</param>
		/// <param name="childMetaRoleGuid">The opposite role,
		/// representing the many end of the set.</param>
		/// <returns>INamedElementDictionary implementation</returns>
		INamedElementDictionary GetCounterpartRoleDictionary(Guid parentMetaRoleGuid, Guid childMetaRoleGuid);
		/// <summary>
		/// A key to test against the transaction context to determine if
		/// duplicate names should be allowed or not. Three values are treated
		/// specially:
		/// NamedElementDictionary.AllowDuplicateNamesKey allows duplicates with
		///    checking the ContextInfo of the current transaction.
		/// NamedElementDictionary.BlockDuplicateNamesKey disallows duplicates with
		///    checking the ContextInfo of the current transaction.
		/// null is interpreted as ModelingDocData.Loading. A null return enables
		///    duplicates during document load, but disallows duplicates in subsequent editing
		/// </summary>
		/// <param name="parentMetaRoleGuid"></param>
		/// <param name="childMetaRoleGuid"></param>
		/// <returns></returns>
		object GetAllowDuplicateNamesContextKey(Guid parentMetaRoleGuid, Guid childMetaRoleGuid);
	}
	#endregion // INamedElementDictionaryOwner interface
	#region INamedElementDictionaryParticipant interface
	/// <summary>
	/// An interface to mark a child element as a participant
	/// in a named element dictionary. This interface should
	/// only be specified on NamedElement-derived classes.
	/// </summary>
	[CLSCompliant(true)]
	public interface INamedElementDictionaryChild
	{
		/// <summary>
		/// Return the role guids to get from a child element
		/// to its containing parent set.
		/// </summary>
		/// <param name="parentMetaRoleGuid"></param>
		/// <param name="childMetaRoleGuid"></param>
		void GetRoleGuids(out Guid parentMetaRoleGuid, out Guid childMetaRoleGuid);
	}
	#endregion // INamedElementDictionaryOwner interface
	#region INamedElementDictionaryLink interface
	/// <summary>
	/// An interface to mark an ElementLink as the relationship
	/// that controls a set of named elements. Technically, this
	/// information can be derived by investigating the role players,
	/// but this is much more work than we want to do whenever an
	/// ElementLink is added.
	/// </summary>
	public interface INamedElementDictionaryLink
	{
		/// <summary>
		/// Get the parent role player
		/// </summary>
		INamedElementDictionaryParent ParentRolePlayer{get;}
		/// <summary>
		/// Get the child role player
		/// </summary>
		INamedElementDictionaryChild ChildRolePlayer { get;}
	}
	#endregion // INamedElementDictionaryLink interface
	#region NamedElementDictionary class
	/// <summary>
	/// A class used to enforce naming across a relationship
	/// representing a collection of NamedElement children.
	/// Duplicate element collection creation, name generation,
	/// and exception handling can all be controlled by derived classes.
	/// </summary>
	public class NamedElementDictionary : INamedElementDictionary
	{
		#region Public token values
		/// <summary>
		/// A large negative number used to make sure the name generation
		/// rules fire very early.
		/// </summary>
		public const int RulePriority = -500000;
		/// <summary>
		/// A key to return from INamedElementDictionaryParent.GetAllowDuplicateNamesContextKey
		/// if duplicate names should be allowed.
		/// </summary>
		public static readonly object AllowDuplicateNamesKey = new object();
		/// <summary>
		/// A key to return from INamedElementDictionaryParent.GetAllowDuplicateNamesContextKey
		/// if duplicate names should not be allowed.
		/// </summary>
		public static readonly object BlockDuplicateNamesKey = new object();
		#endregion // Public token values
		#region Default duplicate collection manager
		/// <summary>
		/// A simple collection implementation using arrays. Does not participate
		/// with IMS in any way.
		/// </summary>
		private class SimpleDuplicateCollectionManager : IDuplicateNameCollectionManager
		{
			/// <summary>
			/// The singleton instance of the collection manager
			/// </summary>
			public static readonly SimpleDuplicateCollectionManager Singleton = new SimpleDuplicateCollectionManager();
			private SimpleDuplicateCollectionManager()
			{
			}
			ICollection IDuplicateNameCollectionManager.OnDuplicateElementAdded(ICollection elementCollection, NamedElement element, bool afterTransaction)
			{
				NamedElement[] elements = (NamedElement[])elementCollection;
				NamedElement[] retVal = null;
				if (elements == null)
				{
					// Create a new collection and prepare for a second call
					retVal = new NamedElement[] { element, null };
				}
				else
				{
					int elementCount = elements.Length;
					if (elementCount == 2 && elements[1] == null)
					{
						// Second half of initial creation
						elements[1] = element;
						retVal = elements;
					}
					else
					{
						// Copy the existing elements and add a new one.
						// Obviously, this makes the assumption that >2 duplicates
						// is unusual. Otherwise, we would use a growable collection
						// instead of reallocating arrays each time.
						retVal = new NamedElement[elementCount + 1];
						elements.CopyTo(retVal, 0);
						retVal[elementCount] = element;
					}
				}
				return retVal;
			}
			ICollection IDuplicateNameCollectionManager.OnDuplicateElementRemoved(ICollection elementCollection, NamedElement element, bool afterTransaction)
			{
				NamedElement[] elements = (NamedElement[])elementCollection;
				int elementCount = elements.Length;
				NamedElement[] retVal = null;
				// There is nothing to do when there are 1 or 2 elements. A call
				// with 2 elements will immediately be followed by a call with
				// 1 element, at which point the collection will be abandoned.
				if (elementCount <= 2)
				{
					retVal = elements;
				}
				else
				{
					retVal = new NamedElement[elementCount - 1];
					int newIndex = 0;
					for (int i = 0; i < elementCount; ++i)
					{
						NamedElement testElement = elements[i];
						if (testElement == element)
						{
							for (int j = i + 1; j < elementCount; ++j)
							{
								// Assume elements are distinct. Continue copy without the test.
								retVal[newIndex] = elements[j];
								++newIndex;
							}
							break;
						}
						else
						{
							retVal[newIndex] = testElement;
							++newIndex;
						}
					}
				}
				return retVal;
			}
		}
		#endregion // Default duplicate collection manager
		#region Member variables
		private Dictionary<string, object> myDictionary;
		private IDuplicateNameCollectionManager myDuplicateManager;
		#endregion // Member variables
		#region Constructors
		/// <summary>
		/// Construct a NamedElementDictionary with the specified duplicate
		/// collection manager
		/// </summary>
		/// <param name="duplicateManager">IDuplicateNameCollectionManager implementation</param>
		public NamedElementDictionary(IDuplicateNameCollectionManager duplicateManager)
		{
			myDuplicateManager = duplicateManager;
			myDictionary = new Dictionary<string, object>();
		}
		/// <summary>
		/// Default constructor. Uses a basic collection manager with no IMS ties
		/// </summary>
		public NamedElementDictionary() : this(SimpleDuplicateCollectionManager.Singleton)
		{
		}
		#endregion // Constructors
		#region Virtual methods
		/// <summary>
		/// Get the root name for generating a unique name for this
		/// element. A unique name is generated by default by appending
		/// numbers to the end of the string. However, the numbers can
		/// be placed elsewhere in the string by including the formatting
		/// sequence '{0}' anywhere in the returned string.
		/// </summary>
		/// <param name="element">The element to generate a name for</param>
		/// <returns>A string to use as the base name. Can include {0} to
		/// indicate where the unique numbers should be included in the string.</returns>
		protected virtual string GetRootNamePattern(NamedElement element)
		{
			string retVal = element.RootName;
			if (retVal == null || retVal.Length == 0)
			{
				retVal = element.GetClassName();
			}
			return retVal;
		}
		/// <summary>
		/// Override to throw a custom exception when
		/// adding duplicate names is not allowed.
		/// </summary>
		/// <param name="element">The element that could not be added due to the duplicate name</param>
		/// <param name="requestedName"></param>
		protected virtual void RaiseDuplicateNameException(NamedElement element, string requestedName)
		{
			// UNDONE: Localize
			throw new InvalidOperationException(string.Format("The name '{0}' is already used in this context.", requestedName));
		}
		#endregion // Virtual methods
		#region Unique name generation
		/// <summary>
		/// Generate a unique name for this element. Defers to
		/// GetRootNamePattern to get a starting name pattern
		/// </summary>
		/// <param name="element">NamedElement</param>
		/// <returns>A name that is not currently in the dictionary.</returns>
		protected string GenerateUniqueName(NamedElement element)
		{
			string rootName = GetRootNamePattern(element);
			if (!rootName.Contains("{0}"))
			{
				rootName += "{0}";
			}
			int i = 0;
			string newKey = null;
			Dictionary<string, object> dic = myDictionary;
			do
			{
				++i;
				newKey = string.Format(rootName, i.ToString());
			} while(dic.ContainsKey(newKey));
			return newKey;
		}
		#endregion // Unique name generation
		#region INamedElementDictionary Members
		void INamedElementDictionary.AddElement(NamedElement element, DuplicateNameAction duplicateAction)
		{
			AddElement(element, duplicateAction);
		}
		/// <summary>
		/// Implements INamedElementDictionary.AddElement
		/// </summary>
		/// <param name="element">NamedElement</param>
		/// <param name="duplicateAction">DuplicateNameAction</param>
		protected void AddElement(NamedElement element, DuplicateNameAction duplicateAction)
		{
			AddElement(element, duplicateAction, element.Name);
		}
		/// <summary>
		/// Add an element with a provided name (ignores the current element name).
		/// Helper function for AddElement and ReplaceElement
		/// </summary>
		/// <param name="element">NamedElement</param>
		/// <param name="duplicateAction">DuplicateNameAction</param>
		/// <param name="elementName">Name to use as the remove key</param>
		private void AddElement(NamedElement element, DuplicateNameAction duplicateAction, string elementName)
		{
			if (elementName.Length == 0)
			{
				// This will fire a name change rule, which will reenter this instance
				// in RenameElement. Just set the name and get out. Don't do this
				// unless we're inside a transaction.
				if (duplicateAction != DuplicateNameAction.RetrieveDuplicateCollection)
				{
					Debug.Assert(element.Store.TransactionManager.InTransaction);
					element.Name = GenerateUniqueName(element);
				}
				return;
			}
			LocatedElement locateData = GetElement(elementName);
			if (locateData.IsEmpty)
			{
				myDictionary.Add(elementName, element);
			}
			else if (duplicateAction == DuplicateNameAction.ThrowOnDuplicateName)
			{
				RaiseDuplicateNameException(element, elementName);
				// The return will only be hit if a derived class chooses
				// to not throw an exception
				return;
			}
			else
			{
				NamedElement singleElement = locateData.SingleElement;
				bool afterTransaction = duplicateAction == DuplicateNameAction.RetrieveDuplicateCollection;
				Debug.Assert(afterTransaction != element.Store.TransactionManager.InTransaction);
				ICollection newCollection = null;
				if (singleElement == null)
				{
					// We already have a collection, just add to it
					ICollection existingCollection = locateData.MultipleElements;
					newCollection = myDuplicateManager.OnDuplicateElementAdded(existingCollection, element, afterTransaction);
					if (object.ReferenceEquals(existingCollection, newCollection))
					{
						// No need to replace
						newCollection = null;
					}
				}
				else
				{
					if (!object.ReferenceEquals(element, singleElement))
					{
						// Call OnDuplicateElementAdded twice. The first time creates the collection,
						// the second one adds to it.
						newCollection = myDuplicateManager.OnDuplicateElementAdded(
							myDuplicateManager.OnDuplicateElementAdded(null, singleElement, afterTransaction),
							element,
							afterTransaction);
					}
				}
				if (newCollection != null)
				{
					myDictionary[elementName] = newCollection;
				}
			}
		}
		void INamedElementDictionary.RemoveElement(NamedElement element, string alternateElementName, DuplicateNameAction duplicateAction)
		{
			RemoveElement(element, alternateElementName, duplicateAction);
		}
		/// <summary>
		/// Implements INamedElementDictionary.RemoveElement, and helper function
		/// for ReplaceEmenet.
		/// Remove an element with a provided name. The current element name is
		/// used if the alternate is not provided.
		/// </summary>
		/// <param name="element">NamedElement</param>
		/// <param name="alternateElementName">If specified, a name to use instead of
		/// the current element name value</param>
		/// <param name="duplicateAction">DuplicateNameAction</param>
		private void RemoveElement(NamedElement element, string alternateElementName, DuplicateNameAction duplicateAction)
		{
			string elementName = alternateElementName;
			if (elementName == null || elementName.Length == 0)
			{
				elementName = element.Name;
			}
			if (elementName.Length != 0)
			{
				LocatedElement locateData = GetElement(elementName);
				if (!locateData.IsEmpty)
				{
					NamedElement singleElement = locateData.SingleElement;
					if (singleElement != null)
					{
						myDictionary.Remove(elementName);
					}
					else
					{
						ICollection existingCollection = locateData.MultipleElements;
						int elementCount = existingCollection.Count;
						bool afterTransaction = duplicateAction == DuplicateNameAction.RetrieveDuplicateCollection;
						Debug.Assert(elementCount >= 2);
						if (elementCount == 2)
						{
							NamedElement otherElement = null;
							foreach (NamedElement testOtherElement in existingCollection)
							{
								if (!object.ReferenceEquals(element, testOtherElement))
								{
									otherElement = testOtherElement;
									break;
								}
							}
							Debug.Assert(otherElement != null);
							myDuplicateManager.OnDuplicateElementRemoved(
								myDuplicateManager.OnDuplicateElementRemoved(existingCollection, element, afterTransaction),
								otherElement,
								afterTransaction);
							myDictionary[elementName] = element;
						}
						else
						{
							ICollection newCollection = myDuplicateManager.OnDuplicateElementRemoved(existingCollection, element, afterTransaction);
							if (!object.ReferenceEquals(newCollection, existingCollection))
							{
								myDictionary[elementName] = newCollection;
							}
						}
					}
				}
			}
		}
		void INamedElementDictionary.ReplaceElement(NamedElement originalElement, NamedElement replacementElement, DuplicateNameAction duplicateAction)
		{
			ReplaceElement(originalElement, replacementElement, duplicateAction);
		}
		/// <summary>
		/// Implements INamedElementDictionary.ReplaceElement
		/// </summary>
		/// <param name="originalElement">NamedElement</param>
		/// <param name="replacementElement">NamedElement</param>
		/// <param name="duplicateAction">DuplicateNameAction</param>
		protected void ReplaceElement(NamedElement originalElement, NamedElement replacementElement, DuplicateNameAction duplicateAction)
		{
			// Consider optimizing if the old/new names are the same
			RemoveElement(originalElement, null, duplicateAction);
			AddElement(replacementElement, duplicateAction);
		}
		void INamedElementDictionary.RenameElement(NamedElement element, string oldName, string newName, DuplicateNameAction duplicateAction)
		{
			RenameElement(element, oldName, newName, duplicateAction);
		}
		/// <summary>
		/// Implements INamedElementDictionary.RenameElement
		/// </summary>
		/// <param name="element">NamedElement</param>
		/// <param name="oldName">string</param>
		/// <param name="newName">string</param>
		/// <param name="duplicateAction">duplicateAction</param>
		protected void RenameElement(NamedElement element, string oldName, string newName, DuplicateNameAction duplicateAction)
		{
			RemoveElement(element, oldName, duplicateAction);
			AddElement(element, duplicateAction, newName);
		}
		LocatedElement INamedElementDictionary.GetElement(string elementName)
		{
			return GetElement(elementName);
		}
		/// <summary>
		/// Return element(s) with the given name
		/// </summary>
		/// <param name="elementName">string</param>
		/// <returns>LocatedElement structure</returns>
		LocatedElement GetElement(string elementName)
		{
			object element;
			return myDictionary.TryGetValue(elementName, out element) ? new LocatedElement(element) : LocatedElement.Empty;
		}
		#endregion // INamedElementDictionary Members
		#region IMS integration
		/// <summary>
		/// Translate the current store context settings into
		/// a duplicate name action setting
		/// </summary>
		/// <param name="element">The element to add</param>
		/// <param name="contextKey">The key to look for</param>
		/// <returns>DuplicateNameAction (ModifyDuplicateCollection or ThrowOnDuplicateName)</returns>
		private static DuplicateNameAction GetDuplicateNameActionForRule(NamedElement element, object contextKey)
		{
			DuplicateNameAction duplicateAction = DuplicateNameAction.ThrowOnDuplicateName;
			if (contextKey == null)
			{
				contextKey = Microsoft.VisualStudio.EnterpriseTools.Shell.ModelingDocData.LoadingKey;
			}
			else if (contextKey == AllowDuplicateNamesKey)
			{
				contextKey = null;
				duplicateAction = DuplicateNameAction.ModifyDuplicateCollection;
			}
			else if (contextKey == BlockDuplicateNamesKey)
			{
				contextKey = null;
			}

			if (contextKey != null)
			{
				if (element.Store.TransactionManager.CurrentTransaction.TopLevelTransaction.Context.ContextInfo.Contains(contextKey))
				{
					duplicateAction = DuplicateNameAction.ModifyDuplicateCollection;
				}
			}
			return duplicateAction;
		}
		[RuleOn(typeof(ElementLink), Priority = NamedElementDictionary.RulePriority)]
		private class ElementLinkAddedRule : AddRule
		{
			public override void ElementAdded(ElementAddedEventArgs e)
			{
				HandleAddRemove(e.ModelElement, false, false);
			}
		}
		[RuleOn(typeof(ElementLink), Priority = NamedElementDictionary.RulePriority)]
		private class ElementLinkRemovedRule : RemoveRule
		{
			public override void ElementRemoved(ElementRemovedEventArgs e)
			{
				HandleAddRemove(e.ModelElement, true, false);
			}
		}
		private static void ElementLinkAddedEvent(object sender, ElementAddedEventArgs e)
		{
			HandleAddRemove(e.ModelElement, false, true);
		}
		private static void ElementLinkRemovedEvent(object sender, ElementRemovedEventArgs e)
		{
			HandleAddRemove(e.ModelElement, true, true);
		}
		/// <summary>
		/// See discussion of the need for NameChangeRecord in the HandleElementChanged
		/// routine.
		/// </summary>
		private struct NameChangeRecord
		{
			public NameChangeRecord(string oldName, string newName)
			{
				OldName = oldName;
				NewName = newName;
			}
			public string OldName;
			public string NewName;
		}
		private static Dictionary<ModelElement, NameChangeRecord> myUnattachedNameChanges;
		private static void ElementEventsEndedEvent(object sender, ElementEventsEndedEventArgs e)
		{
			// Toss unused tracked changes when events are finished
			myUnattachedNameChanges = null;
		}
		private static void HandleAddRemove(ModelElement element, bool remove, bool forEvent)
		{
			if (forEvent)
			{
				UndoManager undoMgr = element.Store.UndoManager;
				if (!undoMgr.InUndoRedo)
				{
					return;
				}
			}
			INamedElementDictionaryLink link = element as INamedElementDictionaryLink;
			if (link != null)
			{
				INamedElementDictionaryParent parent;
				INamedElementDictionaryChild child;
				NamedElement namedChild;
				if ((null != (parent = link.ParentRolePlayer)) &&
					(null != (child = link.ChildRolePlayer)) &&
					(null != (namedChild = child as NamedElement)))
				{
					Guid parentRoleGuid;
					Guid childRoleGuid;
					child.GetRoleGuids(out parentRoleGuid, out childRoleGuid);
					INamedElementDictionary dictionary = parent.GetCounterpartRoleDictionary(parentRoleGuid, childRoleGuid);
					if (dictionary != null)
					{
						DuplicateNameAction duplicateAction;
						string alternateRemoveName = null;
						if (forEvent)
						{
							duplicateAction = DuplicateNameAction.RetrieveDuplicateCollection;
							if (remove && element.IsRemoved && myUnattachedNameChanges != null)
							{
								NameChangeRecord changeRecord;
								if (myUnattachedNameChanges.TryGetValue(namedChild, out changeRecord))
								{
									alternateRemoveName = changeRecord.OldName;
									myUnattachedNameChanges.Remove(element);
								}
							}
						}
						else
						{
							duplicateAction = GetDuplicateNameActionForRule(
								namedChild,
								parent.GetAllowDuplicateNamesContextKey(parentRoleGuid, childRoleGuid));
						}
						if (remove)
						{
							dictionary.RemoveElement(namedChild, alternateRemoveName, duplicateAction);
						}
						else
						{
							dictionary.AddElement(namedChild, duplicateAction);
						}
					}
				}
			}
		}
		// UNDONE: RolePlayerChange
		[RuleOn(typeof(NamedElement), Priority = NamedElementDictionary.RulePriority)]
		private class NamedElementChangedRule : ChangeRule
		{
			public override void ElementAttributeChanged(ElementAttributeChangedEventArgs e)
			{
				HandleElementChanged(e, false);
			}
		}
		private static void NamedElementChangedEvent(object sender, ElementAttributeChangedEventArgs e)
		{
			HandleElementChanged(e, true);
		}
		private static void HandleElementChanged(ElementAttributeChangedEventArgs e, bool forEvent)
		{
			ModelElement element = e.ModelElement;
			if (forEvent)
			{
				UndoManager undoMgr = element.Store.UndoManager;
				if (!undoMgr.InUndoRedo)
				{
					return;
				}
			}
			INamedElementDictionaryChild child = element as INamedElementDictionaryChild;
			if (child != null)
			{
				if (e.MetaAttribute.Id == NamedElement.NameMetaAttributeGuid)
				{
					Guid parentRoleGuid;
					Guid childRoleGuid;
					child.GetRoleGuids(out parentRoleGuid, out childRoleGuid);
					NamedElement namedChild = child as NamedElement;
					IList parents = namedChild.GetCounterpartRolePlayers(childRoleGuid, parentRoleGuid);
					int parentsCount = parents.Count;
					if (parentsCount == 0 && forEvent && element.IsRemoved)
					{
						// Handle a problem with events. The counterpart collection is always empty
						// when we get here during events, so there is no way to get back to the parent object
						// until we get the remove event for the element. However, the remove event will get
						// the element with the new value, not the old. Track this change.
						NameChangeRecord changeRecord = new NameChangeRecord(e.OldValue as string, e.NewValue as string);
						if (changeRecord.OldName != null)
						{
							if (myUnattachedNameChanges == null)
							{
								myUnattachedNameChanges = new Dictionary<ModelElement, NameChangeRecord>();
							}
							NameChangeRecord existingRecord;
							if (myUnattachedNameChanges.TryGetValue(namedChild, out existingRecord))
							{
								Debug.Assert(existingRecord.NewName == changeRecord.OldName);
								changeRecord.OldName = existingRecord.OldName;
							}
							myUnattachedNameChanges[namedChild] = changeRecord;
						}
					}
					for (int i = 0; i < parentsCount; ++i)
					{
						INamedElementDictionaryParent parent = parents[i] as INamedElementDictionaryParent;
						if (parent != null)
						{
							INamedElementDictionary dictionary = parent.GetCounterpartRoleDictionary(parentRoleGuid, childRoleGuid);
							if (dictionary != null)
							{
								DuplicateNameAction duplicateAction;
								if (forEvent)
								{
									duplicateAction = DuplicateNameAction.RetrieveDuplicateCollection;
								}
								else
								{
									duplicateAction = GetDuplicateNameActionForRule(
										namedChild,
										parent.GetAllowDuplicateNamesContextKey(parentRoleGuid, childRoleGuid));
								}
								dictionary.RenameElement(namedChild, e.OldValue as string, e.NewValue as string, duplicateAction);
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// Call from ModelingDocData.AddPostLoadModelingEventHandlers to
		/// attach handlers that correctly deal with undo and redo scenarios.
		/// </summary>
		/// <param name="store">The store to attach to</param>
		public static void AttachedEventHandlers(Store store)
		{
			MetaDataDirectory dataDirectory = store.MetaDataDirectory;
			EventManagerDirectory eventDirectory = store.EventManagerDirectory;
			MetaClassInfo classInfo = dataDirectory.FindMetaRelationship(ElementLink.MetaRelationshipGuid);

			// Track ElementLink changes
			eventDirectory.ElementAdded.Add(classInfo, new ElementAddedEventHandler(ElementLinkAddedEvent));
			eventDirectory.ElementRemoved.Add(classInfo, new ElementRemovedEventHandler(ElementLinkRemovedEvent));
			// UNDONE: RolePlayerChanged

			// Track NamedElement 
			classInfo = dataDirectory.FindMetaClass(NamedElement.MetaClassGuid);
			eventDirectory.ElementAttributeChanged.Add(classInfo, new ElementAttributeChangedEventHandler(NamedElementChangedEvent));

			eventDirectory.ElementEventsEnded.Add(new ElementEventsEndedEventHandler(ElementEventsEndedEvent));
		}
		/// <summary>
		/// Call from ModelingDocData.RemoveModelingEventHandlers to detach
		/// handlers from the store.
		/// </summary>
		/// <param name="store">The store to detach from</param>
		public static void DetachEventHandlers(Store store)
		{
			MetaDataDirectory dataDirectory = store.MetaDataDirectory;
			EventManagerDirectory eventDirectory = store.EventManagerDirectory;
			MetaClassInfo classInfo = dataDirectory.FindMetaRelationship(ElementLink.MetaRelationshipGuid);

			// Track ElementLink changes
			eventDirectory.ElementAdded.Remove(classInfo, new ElementAddedEventHandler(ElementLinkAddedEvent));
			eventDirectory.ElementRemoved.Remove(classInfo, new ElementRemovedEventHandler(ElementLinkRemovedEvent));
			// UNDONE: RolePlayerChanged

			// Track NamedElement 
			classInfo = dataDirectory.FindMetaClass(NamedElement.MetaClassGuid);
			eventDirectory.ElementAttributeChanged.Remove(classInfo, new ElementAttributeChangedEventHandler(NamedElementChangedEvent));

			eventDirectory.ElementEventsEnded.Remove(new ElementEventsEndedEventHandler(ElementEventsEndedEvent));
		}
		#endregion // IMS integration
	}
	#endregion // NamedElementDictionary class
}
