﻿/************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2010-2012 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   This program can be provided to you by Xceed Software Inc. under a
   proprietary commercial license agreement for use in non-Open Source
   projects. The commercial version of Extended WPF Toolkit also includes
   priority technical support, commercial updates, and many additional 
   useful WPF controls if you license Xceed Business Suite for WPF.

   Visit http://xceed.com and follow @datagrid on Twitter.

  **********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Xceed.Utils.Collections;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Xceed.Wpf.DataGrid
{
  public sealed class DataGridVirtualizingCollectionView : DataGridVirtualizingCollectionViewBase
  {
    #region CONSTRUCTORS

    public DataGridVirtualizingCollectionView()
      : this( null, typeof( object ), true, DataGridVirtualizingCollectionViewBase.DefaultPageSize, DataGridVirtualizingCollectionViewBase.DefaultMaxRealizedItemCount )
    {
    }

    public DataGridVirtualizingCollectionView( Type itemType )
      : this( null, itemType, true, DataGridVirtualizingCollectionViewBase.DefaultPageSize, DataGridVirtualizingCollectionViewBase.DefaultMaxRealizedItemCount )
    {
    }

    public DataGridVirtualizingCollectionView( Type itemType, bool autoCreateItemProperties, int pageSize, int maxRealizedItemCount )
      : this( null, itemType, autoCreateItemProperties, pageSize, maxRealizedItemCount )
    {
    }


    public DataGridVirtualizingCollectionView( DataTable sourceSchema )
      : this( sourceSchema, typeof( object ), true, DataGridVirtualizingCollectionViewBase.DefaultPageSize, DataGridVirtualizingCollectionViewBase.DefaultMaxRealizedItemCount )
    {
    }

    public DataGridVirtualizingCollectionView( DataTable sourceSchema, bool autoCreateItemProperties, int pageSize, int maxRealizedItemCount )
      : this( sourceSchema, typeof( object ), autoCreateItemProperties, pageSize, maxRealizedItemCount )
    {
    }


    private DataGridVirtualizingCollectionView( DataTable sourceSchema, Type itemType, bool autoCreateItemProperties, int pageSize, int maxRealizedItemCount )
      : base( sourceSchema, itemType, autoCreateItemProperties, pageSize, maxRealizedItemCount )
    {
    }

    #endregion CONSTRUCTORS


    #region DATA VIRTUALIZATION

    public event EventHandler<QueryItemCountEventArgs> QueryItemCount;

    internal int OnQueryItemCount( DataGridVirtualizingCollectionViewGroup collectionViewGroup )
    {
      QueryItemCountEventArgs e = new QueryItemCountEventArgs( this, collectionViewGroup );

      if( this.QueryItemCount != null )
        this.QueryItemCount( this, e );

      DataGridVirtualizingCollectionViewSource source = this.ParentCollectionViewSourceBase as DataGridVirtualizingCollectionViewSource;

      if( source != null )
        source.OnQueryItemCount( e );

      return e.Count;
    }

    public event EventHandler<QueryGroupsEventArgs> QueryGroups;

    internal List<GroupNameCountPair> OnQueryGroups( DataGridVirtualizingCollectionViewGroup parentGroup )
    {
      ObservableCollection<GroupDescription> groupDescriptions = this.GroupDescriptions;
      int nextLevel = parentGroup.Level + 1;

      Debug.Assert( ( groupDescriptions != null ) && ( groupDescriptions.Count > nextLevel ) );

      QueryGroupsEventArgs e = new QueryGroupsEventArgs( this, parentGroup, groupDescriptions[ nextLevel ] );

      if( this.QueryGroups != null )
        this.QueryGroups( this, e );

      DataGridVirtualizingCollectionViewSource source = this.ParentCollectionViewSourceBase as DataGridVirtualizingCollectionViewSource;

      if( source != null )
        source.OnQueryGroups( e );

      return e.ChildGroupNameCountPairs;
    }


    public event EventHandler<QueryItemsEventArgs> AbortQueryItems;

    internal void OnAbortQueryItems( AsyncQueryInfo asyncQueryInfo, DataGridVirtualizingCollectionViewGroup collectionViewGroup )
    {
      QueryItemsEventArgs e = new QueryItemsEventArgs( this, collectionViewGroup, asyncQueryInfo );

      if( this.AbortQueryItems != null )
        this.AbortQueryItems( this, e );

      DataGridVirtualizingCollectionViewSource source = this.ParentCollectionViewSourceBase as DataGridVirtualizingCollectionViewSource;

      if( source != null )
        source.OnAbortQueryItems( e );
    }

    public event EventHandler<QueryItemsEventArgs> QueryItems;

    internal void OnQueryItems( AsyncQueryInfo asyncQueryInfo, DataGridVirtualizingCollectionViewGroup collectionViewGroup )
    {
      QueryItemsEventArgs e = new QueryItemsEventArgs( this, collectionViewGroup, asyncQueryInfo );

      if( this.QueryItems != null )
        this.QueryItems( this, e );

      DataGridVirtualizingCollectionViewSource source = this.ParentCollectionViewSourceBase as DataGridVirtualizingCollectionViewSource;

      if( source != null )
        source.OnQueryItems( e );
    }


    internal event EventHandler<QueryAutoFilterDistinctValuesEventArgs> QueryAutoFilterDistinctValues;

    internal void OnQueryAutoFilterDistinctValues( QueryAutoFilterDistinctValuesEventArgs e )
    {
      if( this.QueryAutoFilterDistinctValues != null )
        this.QueryAutoFilterDistinctValues( this, e );

      DataGridVirtualizingCollectionViewSource source = this.ParentCollectionViewSourceBase as DataGridVirtualizingCollectionViewSource;

      if( source != null )
        source.OnQueryAutoFilterDistinctValues( e );
    }

    #endregion DATA VIRTUALIZATION

    internal override IEnumerator GetVirtualEnumerator()
    {
      return ( ( DataGridVirtualizingCollectionViewGroupRoot )this.RootGroup ).GetVirtualPageManager().GetEnumerator();
    }

    internal override void RefreshDistinctValuesForField( DataGridItemPropertyBase dataGridItemProperty )
    {
      if( dataGridItemProperty == null )
        return;

      if( dataGridItemProperty.CalculateDistinctValues == false )
        return;

      // List containing current column distinct values
      HashSet<object> currentColumnDistinctValues = new HashSet<object>();

      ReadOnlyObservableHashList readOnlyColumnDistinctValues = null;

      // If the key is not set in DistinctValues yet, do not calculate distinct values for this field
      if( !( ( DistinctValuesDictionary )this.DistinctValues ).InternalTryGetValue( dataGridItemProperty.Name, out readOnlyColumnDistinctValues ) )
        return;

      ObservableHashList columnDistinctValues = readOnlyColumnDistinctValues.InnerObservableHashList;

      // We use the DistinctValuesSortComparer if present, else the SortComparer for the DataGridItemProperty, else, 
      // the Comparer used is the one of the base class.
      IComparer distinctValuesSortComparer = dataGridItemProperty.DistinctValuesSortComparer;

      if( distinctValuesSortComparer == null )
        distinctValuesSortComparer = dataGridItemProperty.SortComparer;

      using( columnDistinctValues.DeferINotifyCollectionChanged() )
      {
        QueryAutoFilterDistinctValuesEventArgs e = new QueryAutoFilterDistinctValuesEventArgs( dataGridItemProperty );
        this.OnQueryAutoFilterDistinctValues( e );

        foreach( object distinctValue in e.DistinctValues )
        {
          // Compute current value to be able to remove unused values
          currentColumnDistinctValues.Add( distinctValue );
        }

        DataGridCollectionViewBase.RemoveUnusedDistinctValues( 
          distinctValuesSortComparer, currentColumnDistinctValues, columnDistinctValues, null );
      }
    }

    internal override DataGridVirtualizingCollectionViewGroupBase CreateNewRootGroup()
    {
      bool rootIsBottomLevel = ( this.GroupDescriptions == null ) ? true : ( this.GroupDescriptions.Count == 0 );

      return new DataGridVirtualizingCollectionViewGroupRoot( this, rootIsBottomLevel );
    }
  }
}