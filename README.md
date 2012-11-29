ProjectedCollections
====================

This project provides an implementation of ObservableCollection<T> which can watch a source BindingList or ObservableCollection<T>, and transform the source elements into it's own target elements utilizing a user-provided transformation.

Usage
-----

new ProjectedObservableCollection(sourceModelCollection, model => createViewModel(model));