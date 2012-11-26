// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectedObservableCollection.cs" company="Liebherr International AG">
//   © 2012 Liebherr. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ProjectedCollections
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Implements a specialization of the <see cref="ObservableCollection{T}"/> which will receive an injected collection,
    /// and keep itself in sync with the source collection. The source collection can be another <see cref="ObservableCollection{T}"/>
    /// or a <see cref="BindingList{T}"/>
    /// </summary>
    /// <typeparam name="T">
    /// The type of the items in the source collection.
    /// </typeparam>
    /// <typeparam name="TProjected">
    /// The type of the projected items in this collection.
    /// </typeparam>
    public class ProjectedObservableCollection<T, TProjected> : ObservableCollection<TProjected>, 
                                                                IDisposable
    {
        #region Fields

        /// <summary>
        /// Defines the function to be used to project the source items into a target item of type <see cref="TProjected"/>.
        /// </summary>
        private readonly Func<T, TProjected> projection;

        /// <summary>
        /// Defines the source collection of type <see cref="BindingList{T}"/>.
        /// </summary>
        private readonly BindingList<T> sourceBindingList;

        /// <summary>
        /// Defines the source collection of type <see cref="ObservableCollection{T}"/>.
        /// </summary>
        private readonly ObservableCollection<T> sourceObservableCollection;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedObservableCollection{T,TProjected}"/> class.
        /// </summary>
        /// <param name="sourceObservableCollection">
        /// The source collection of type <see cref="ObservableCollection{T}"/>.
        /// </param>
        /// <param name="projection">
        /// Defines the function to be used to project the source items into a target item of type <see cref="TProjected"/>.
        /// </param>
        public ProjectedObservableCollection(ObservableCollection<T> sourceObservableCollection, 
                                             Func<T, TProjected> projection)
            : base(sourceObservableCollection.Select(projection))
        {
            this.projection = projection;

            this.sourceObservableCollection = sourceObservableCollection;
            this.sourceObservableCollection.CollectionChanged += this.SourceCollectionOnCollectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectedObservableCollection{T,TProjected}"/> class.
        /// </summary>
        /// <param name="sourceBindingList">
        /// The source collection of type <see cref="BindingList{T}"/>.
        /// </param>
        /// <param name="projection">
        /// Defines the function to be used to project the source items into a target item of type <see cref="TProjected"/>.
        /// </param>
        public ProjectedObservableCollection(BindingList<T> sourceBindingList, 
                                             Func<T, TProjected> projection)
            : base(sourceBindingList.Select(projection))
        {
            this.projection = projection;

            this.sourceBindingList = sourceBindingList;
            this.sourceBindingList.ListChanged += this.SourceBindingListOnListChanged;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Overrides the base implementation to implement disposal of projected items.
        /// </summary>
        protected override void ClearItems()
        {
            foreach (var projectedItem in this.OfType<IDisposable>())
            {
                projectedItem.Dispose();
            }

            base.ClearItems();
        }

        /// <summary>
        ///   Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"> <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of managed resources
                if (this.sourceObservableCollection != null)
                {
                    this.sourceObservableCollection.CollectionChanged -= this.SourceCollectionOnCollectionChanged;
                }

                if (this.sourceBindingList != null)
                {
                    this.sourceBindingList.ListChanged += this.SourceBindingListOnListChanged;
                }
            }

            // Dispose of native resources
        }

        /// <summary>
        /// Overrides the base implementation to implement disposal of projected items.
        /// </summary>
        /// <param name="args">
        /// The <see cref="NotifyCollectionChangedEventArgs"/> which describes the change on this collection.
        /// </param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            base.OnCollectionChanged(args);

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    foreach (var oldItem in args.OldItems.OfType<IDisposable>())
                    {
                        oldItem.Dispose();
                    }

                    break;
            }
        }

        /// <summary>
        /// Event handler which gets called whenever the <see cref="sourceBindingList"/> collection is changed (through <see cref="BindingList{T}.ListChanged"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="sourceBindingList"/>.
        /// </param>
        /// <param name="args">
        /// The <see cref="ListChangedEventArgs"/> which describes the change on the <see cref="sourceBindingList"/>.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// Thrown if the <see cref="ListChangedEventArgs.ListChangedType"/> is <see cref="ListChangedType.ItemMoved"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the <see cref="ListChangedEventArgs.ListChangedType"/> is unknown.
        /// </exception>
        private void SourceBindingListOnListChanged(object sender, 
                                                    ListChangedEventArgs args)
        {
            var bindingList = (BindingList<T>)sender;

            switch (args.ListChangedType)
            {
                case ListChangedType.Reset:
                    this.Clear();

                    foreach (var projectedItem in bindingList.Select(this.projection))
                    {
                        this.Add(projectedItem);
                    }

                    break;
                case ListChangedType.ItemAdded:
                    this.Add(this.projection(bindingList[args.NewIndex]));
                    break;
                case ListChangedType.ItemDeleted:
                    this.RemoveAt(args.OldIndex);
                    break;
                case ListChangedType.ItemMoved:
                    throw new NotImplementedException();
                case ListChangedType.ItemChanged:
                    break;
                case ListChangedType.PropertyDescriptorAdded:
                    break;
                case ListChangedType.PropertyDescriptorDeleted:
                    break;
                case ListChangedType.PropertyDescriptorChanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Event handler which gets called whenever the <see cref="sourceObservableCollection"/> collection is changed (through <see cref="ObservableCollection{T}.CollectionChanged"/>.
        /// </summary>
        /// <param name="sender">
        /// The <see cref="sourceObservableCollection"/>.
        /// </param>
        /// <param name="args">
        /// The <see cref="NotifyCollectionChangedEventArgs"/> which describes the change on the <see cref="sourceObservableCollection"/>.
        /// </param>
        /// <exception cref="NotImplementedException">
        /// Thrown if:
        /// - The <see cref="NotifyCollectionChangedEventArgs.Action"/> is <see cref="NotifyCollectionChangedAction.Move"/>;
        /// - The <see cref="NotifyCollectionChangedEventArgs.Action"/> is <see cref="NotifyCollectionChangedAction.Remove"/>, and the <see cref="NotifyCollectionChangedEventArgs.OldStartingIndex"/> is <c>-1</c>.
        /// - The <see cref="NotifyCollectionChangedEventArgs.Action"/> is <see cref="NotifyCollectionChangedAction.Replace"/>, and either the <see cref="NotifyCollectionChangedEventArgs.NewStartingIndex"/> is <c>-1</c> or it is not the same as <see cref="NotifyCollectionChangedEventArgs.OldStartingIndex"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if the <see cref="NotifyCollectionChangedEventArgs.Action"/> is unknown.
        /// </exception>
        private void SourceCollectionOnCollectionChanged(object sender, 
                                                         NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var index = args.NewStartingIndex;

                    if (index == -1)
                    {
                        index = this.Count;
                    }

                    foreach (var newItem in args.NewItems.Cast<T>())
                    {
                        this.Insert(index++, this.projection(newItem));
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                    if (args.OldStartingIndex == -1)
                    {
                        throw new NotImplementedException("Cannot remove items from projected collection when index is unknown.");
                    }

                    for (var index = args.OldStartingIndex + args.OldItems.Count - 1; index >= args.OldStartingIndex; --index)
                    {
                        this.RemoveAt(index);
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                {
                    if (args.NewStartingIndex == -1 || args.NewStartingIndex != args.OldStartingIndex)
                    {
                        throw new NotImplementedException("Cannot replace items from projected collection when index is unknown or not the same as the old index.");
                    }

                    var index = args.NewStartingIndex;

                    foreach (var newItem in args.NewItems.Cast<T>())
                    {
                        this.SetItem(index++, this.projection(newItem));
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Reset:
                    this.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}