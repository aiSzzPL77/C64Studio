﻿using C64Studio.CustomRenderer;
using C64Studio.Displayer;
using C64Studio.Formats;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using C64Studio.Types;

namespace C64Studio
{
  public partial class ValueTableEditor : BaseDocument
  {
    private ValueTableProject         m_Project = new ValueTableProject();

    private Parser.ASMFileParser      m_Parser = new Parser.ASMFileParser();



    public ValueTableEditor( StudioCore Core )
    {
      this.Core = Core;
      DocumentInfo.Type = ProjectElement.ElementType.VALUE_TABLE;
      DocumentInfo.UndoManager.MainForm = Core.MainForm;

      m_IsSaveable = true;
      InitializeComponent();

      checkExportToDataIncludeRes.Checked = true;
      checkExportToDataWrap.Checked = true;
      comboExportRange.Items.Add( "All" );
      comboExportRange.Items.Add( "Selection" );
      comboExportRange.Items.Add( "Range" );
      comboExportRange.SelectedIndex = 0;

      RefreshDisplayOptions();
    }



    public override void RefreshDisplayOptions()
    {
      /*
      tabEditor.BackColor = ( (LightToolStripRenderer)ToolStripManager.Renderer ).BackColor;

      foreach ( TabPage tab in tabSpriteEditor.TabPages )
      {
        tab.BackColor = ( (LightToolStripRenderer)ToolStripManager.Renderer ).BackColor;
      }*/
    }



    private new bool Modified
    {
      get
      {
        return base.Modified;
      }
      set
      {
        if ( value )
        {
          SetModified();
        }
        else
        {
          SetUnmodified();
        }
        saveValueTableProjectToolStripMenuItem.Enabled = Modified;
      }
    }



    private void openToolStripMenuItem_Click( object sender, EventArgs e )
    {
      string filename;

      if ( OpenFile( "Open Value Table Project or File", Types.Constants.FILEFILTER_VALUE_TABLE_PROJECT + Types.Constants.FILEFILTER_ALL, out filename ) )
      {
        ImportValueTableProject( filename );
      }
    }



    public void Clear()
    {
      m_Project.Clear();
    }



    public bool ImportValueTableProject( string Filename )
    {
      Clear();

      GR.Memory.ByteBuffer projectFile = GR.IO.File.ReadAllBytes( Filename );
      if ( projectFile == null )
      {
        return false;
      }
      if ( !m_Project.ReadFromBuffer( projectFile ) )
      {
        return false;
      }

      // TODO - controls
      foreach ( var value in m_Project.ValueTable.Values )
      {
        listValues.Items.Add( value );
      }
      editStartValue.Text     = m_Project.ValueTable.StartValue;
      editEndValue.Text       = m_Project.ValueTable.EndValue;
      editStepValue.Text      = m_Project.ValueTable.StepValue;
      editValueFunction.Text  = m_Project.ValueTable.Formula;

      Modified = false;

      if ( DocumentInfo.Element == null )
      {
        DocumentInfo.DocumentFilename = Filename;
      }

      saveValueTableProjectToolStripMenuItem.Enabled = true;
      closeValueTableProjectToolStripMenuItem.Enabled = true;
      return true;
    }



    public override bool Load()
    {
      if ( string.IsNullOrEmpty( DocumentInfo.DocumentFilename ) )
      {
        return false;
      }
      try
      {
        ImportValueTableProject( DocumentInfo.FullPath );
      }
      catch ( System.IO.IOException ex )
      {
        System.Windows.Forms.MessageBox.Show( "Could not load value table project file " + DocumentInfo.FullPath + ".\r\n" + ex.Message, "Could not load file" );
        return false;
      }
      SetUnmodified();
      return true;
    }



    private bool SaveProject( bool SaveAs )
    {
      string    saveFilename = DocumentInfo.FullPath;

      if ( ( string.IsNullOrEmpty( DocumentInfo.DocumentFilename ) )
      ||   ( SaveAs ) )
      {
        System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

        saveDlg.Title = "Save Value Table Project as";
        saveDlg.Filter = "Value Table Projects|*.valuetableproject|All Files|*.*";
        if ( DocumentInfo.Project != null )
        {
          saveDlg.InitialDirectory = DocumentInfo.Project.Settings.BasePath;
        }
        if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
        {
          return false;
        }
        saveFilename = saveDlg.FileName;
        if ( !SaveAs )
        {
          DocumentInfo.DocumentFilename = saveDlg.FileName;
          if ( DocumentInfo.Element != null )
          {
            DocumentInfo.DocumentFilename = GR.Path.RelativePathTo( saveDlg.FileName, false, System.IO.Path.GetFullPath( DocumentInfo.Project.Settings.BasePath ), true );
            DocumentInfo.Element.Name = System.IO.Path.GetFileNameWithoutExtension( DocumentInfo.DocumentFilename );
            DocumentInfo.Element.Node.Text = System.IO.Path.GetFileName( DocumentInfo.DocumentFilename );
            DocumentInfo.Element.Filename = DocumentInfo.DocumentFilename;
          }
          saveFilename = DocumentInfo.FullPath;
        }
      }

      GR.Memory.ByteBuffer dataToSave = SaveToBuffer();
      if ( !GR.IO.File.WriteAllBytes( saveFilename, dataToSave ) )
      {
        return false;
      }
      Modified = false;
      return true;
    }



    private void closeCharsetProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( DocumentInfo.DocumentFilename == "" )
      {
        return;
      }
      if ( Modified )
      {
        DialogResult doSave = MessageBox.Show( "There are unsaved changes in your value table project. Save now?", "Save changes?", MessageBoxButtons.YesNoCancel );
        if ( doSave == DialogResult.Cancel )
        {
          return;
        }
        if ( doSave == DialogResult.Yes )
        {
          SaveProject( false );
        }
      }
      Clear();
      DocumentInfo.DocumentFilename = "";
      Modified = false;

      closeValueTableProjectToolStripMenuItem.Enabled = false;
      saveValueTableProjectToolStripMenuItem.Enabled = false;
    }



    private void saveCharsetProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      SaveProject( false );
    }



    public override bool Save()
    {
      return SaveProject( false );
    }



    public override bool SaveAs()
    {
      return SaveProject( true );
    }



    public override GR.Memory.ByteBuffer SaveToBuffer()
    {
      return m_Project.SaveToBuffer();
    }



    private void btnExportSprite_Click( object sender, EventArgs e )
    {
      /*
      var exportIndices = GetExportIndices();
      if ( exportIndices.Count == 0 )
      {
        return;
      }

      System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

      saveDlg.Title = "Export Sprites to";
      saveDlg.Filter = "Sprites|*.spr|All Files|*.*";
      saveDlg.FileName = m_SpriteProject.ExportFilename;
      if ( DocumentInfo.Project != null )
      {
        saveDlg.InitialDirectory = DocumentInfo.Project.Settings.BasePath;
      }
      if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return;
      }
      if ( m_SpriteProject.ExportFilename != saveDlg.FileName )
      {
        m_SpriteProject.ExportFilename = saveDlg.FileName;
        Modified = true;
      }

      GR.Memory.ByteBuffer spriteData = new GR.Memory.ByteBuffer();
      for ( int i = 0; i < exportIndices.Count; ++i )
      {
        spriteData.Append( m_SpriteProject.Sprites[exportIndices[i]].Data );
        spriteData.AppendU8( (byte)m_SpriteProject.Sprites[exportIndices[i]].Color );
      }
      GR.IO.File.WriteAllBytes( m_SpriteProject.ExportFilename, spriteData );*/
    }



    private void btnExportSpriteToData_Click( object sender, EventArgs e )
    {
      /*
      var exportIndices = GetExportIndices();
      if ( exportIndices.Count == 0 )
      {
        return;
      }

      int wrapByteCount = GR.Convert.ToI32( editWrapByteCount.Text );
      if ( wrapByteCount <= 0 )
      {
        wrapByteCount = 8;
      }
      string prefix = editPrefix.Text;

      GR.Memory.ByteBuffer exportData = new GR.Memory.ByteBuffer();
      for ( int i = 0; i < exportIndices.Count; ++i )
      {
        exportData.Append( m_SpriteProject.Sprites[exportIndices[i]].Data );

        byte color = (byte)m_SpriteProject.Sprites[exportIndices[i]].Color;
        if ( m_SpriteProject.Sprites[exportIndices[i]].Multicolor )
        {
          color |= 0x80;
        }
        exportData.AppendU8( color );
      }

      bool wrapData = checkExportToDataWrap.Checked;
      bool prefixRes = checkExportToDataIncludeRes.Checked;
      if ( !prefixRes )
      {
        prefix = "";
      }

      string line = Util.ToASMData( exportData, wrapData, wrapByteCount, prefix );
      editDataExport.Text = line;
      */
    }



    private void checkExportToDataWrap_CheckedChanged( object sender, EventArgs e )
    {
      editWrapByteCount.Enabled = checkExportToDataWrap.Checked;
    }



    private void checkExportToDataIncludeRes_CheckedChanged( object sender, EventArgs e )
    {
      editPrefix.Enabled = checkExportToDataIncludeRes.Checked;
    }



    private void btnImportSprite_Click( object sender, EventArgs e )
    {
      /*
      string filename;

      if ( OpenFile( "Open sprite file", Types.Constants.FILEFILTER_SPRITE + Types.Constants.FILEFILTER_ALL, out filename ) )
      {
        ImportSprites( filename, true, true );
      }*/
    }



    private void editDataExport_KeyPress( object sender, KeyPressEventArgs e )
    {
      if ( ( System.Windows.Forms.Control.ModifierKeys == Keys.Control )
      &&   ( e.KeyChar == 1 ) )
      {
        editDataExport.SelectAll();
        e.Handled = true;
      }
    }



    private void comboExportRange_SelectedIndexChanged( object sender, EventArgs e )
    {
      labelCharactersFrom.Enabled = ( comboExportRange.SelectedIndex == 2 );
      editSpriteFrom.Enabled = ( comboExportRange.SelectedIndex == 2 );
      labelCharactersTo.Enabled = ( comboExportRange.SelectedIndex == 2 );
      editSpriteCount.Enabled = ( comboExportRange.SelectedIndex == 2 );
    }



    private void editValueEntry_TextChanged( object sender, EventArgs e )
    {
      if ( listValues.SelectedIndices.Count == 0 )
      {
        return;
      }

      int     newIndex = listValues.SelectedIndices[0];

      if ( listValues.Items[newIndex].Text != editValueEntry.Text )
      {
        listValues.Items[newIndex].Text = editValueEntry.Text;
        SetModified();
      }
    }



    private ListViewItem listValues_AddingItem( object sender )
    {
      var item = new ListViewItem( editValueEntry.Text );

      return item;
    }



    private void listValues_ItemAdded( object sender, ListViewItem Item )
    {
      ValuesChanged();
    }



    private void listValues_ItemMoved( object sender, ListViewItem Item1, ListViewItem Item2 )
    {
      ValuesChanged();
    }



    private void listValues_ItemRemoved( object sender, ListViewItem Item )
    {
      ValuesChanged();
    }



    private void ValuesChanged()
    {
      m_Project.ValueTable.Values.Clear();

      foreach ( ListViewItem item in listValues.Items )
      {
        m_Project.ValueTable.Values.Add( item.Text );
      }
      SetModified();
    }



    private void listValues_SelectedIndexChanged( object sender, ListViewItem Item )
    {
      if ( listValues.SelectedIndices.Count == 0 )
      {
        return;
      }
      int     newIndex = listValues.SelectedIndices[0];

      editValueEntry.Text = listValues.Items[newIndex].Text;
    }



    private void editValueFunction_TextChanged( object sender, EventArgs e )
    {
      if ( editValueFunction.Text != m_Project.ValueTable.Formula )
      {
        m_Project.ValueTable.Formula = editValueFunction.Text;
        SetModified();
      }
      GeneratorValuesChanged();
    }



    private void editStartValue_TextChanged( object sender, EventArgs e )
    {
      if ( editStartValue.Text != m_Project.ValueTable.StartValue )
      {
        m_Project.ValueTable.StartValue = editStartValue.Text;
        SetModified();
      }
      GeneratorValuesChanged();
    }



    private void editEndValue_TextChanged( object sender, EventArgs e )
    {
      if ( editEndValue.Text != m_Project.ValueTable.EndValue )
      {
        m_Project.ValueTable.EndValue = editEndValue.Text;
        SetModified();
      }
      GeneratorValuesChanged();
    }



    private void editStepValue_TextChanged( object sender, EventArgs e )
    {
      if ( editStepValue.Text != m_Project.ValueTable.StepValue )
      {
        m_Project.ValueTable.StepValue = editStepValue.Text;
        SetModified();
      }
      GeneratorValuesChanged();
    }



    private void GeneratorValuesChanged()
    {
      if ( !checkAutomatedGeneration.Checked )
      {
        return;
      }
      GenerateValues();
    }



    private void GenerateValues()
    {
      m_Parser = new Parser.ASMFileParser();

      m_Parser.AddExtFunction( "sin", 1, 1, ExtSinus );

      double  startValue = Util.StringToDouble( m_Project.ValueTable.StartValue );
      double  endValue = Util.StringToDouble( m_Project.ValueTable.EndValue );
      double  stepValue = Util.StringToDouble( m_Project.ValueTable.StepValue );

      if ( ( startValue != endValue )
      &&   ( stepValue == 0 ) )
      {
        SetError( "Step value must not be equal zero" );
        return;
      }
      if ( ( startValue <= endValue )
      &&   ( stepValue < 0 ) )
      {
        SetError( "Step value must be positive" );
        return;
      }
      if ( ( startValue >= endValue )
      &&   ( stepValue > 0 ) )
      {
        SetError( "Step value must be negative" );
        return;
      }

      if ( checkClearPreviousValues.Checked )
      {
        m_Project.ValueTable.Values.Clear();
        listValues.Items.Clear();
      }

      double  curValue = startValue;
      bool    completed = false;

      do
      {
        m_Parser.AddConstantF( "x", curValue, 0, "", 0, 1 );
        var tokens = m_Parser.ParseTokenInfo( m_Project.ValueTable.Formula, 0, m_Project.ValueTable.Formula.Length );
        if ( tokens == null )
        {
          SetError( m_Parser.GetError().Code.ToString() );
          return;
        }
        double result = 0.0;
        if ( !m_Parser.EvaluateTokensNumeric( 0, tokens, out result ) )
        {
          SetError( m_Parser.GetError().Code.ToString() );
          return;
        }

        m_Project.ValueTable.Values.Add( result.ToString() );
        listValues.Items.Add( result.ToString() );

        curValue += stepValue;

        if ( startValue == endValue )
        {
          completed = true;
        }
        if ( ( stepValue > 0 )
        &&   ( curValue > endValue ) )
        {
          completed = true;
        }
        if ( ( stepValue < 0 )
        &&   ( curValue < endValue ) )
        {
          completed = true;
        }
      }
      while ( !completed );

      SetError( "OK" );
    }



    private List<TokenInfo> ExtSinus( List<TokenInfo> Arguments )
    {
      var result = new List<TokenInfo>();

      if ( Arguments.Count != 1 )
      {
        SetError( "Invalid argument count" );
        return result;
      }
      double functionResult = 0;
      if ( !m_Parser.EvaluateTokensNumeric( 0, Arguments, out functionResult ) )
      {
        SetError( "Invalid argument" );
        return result;
      }
      var resultValue = new TokenInfo()
      {
        Type = TokenInfo.TokenType.LITERAL_NUMBER,
        Content = Math.Sin( functionResult * Math.PI / 180.0f ).ToString( "0.00000000000000000000", System.Globalization.CultureInfo.InvariantCulture )
      };
      result.Add( resultValue );
      return result;
    }



    private void SetError( string Message )
    {
      labelGenerationResult.Text = Message;
      if ( Message != "OK" )
      {
        labelGenerationResult.ForeColor = System.Drawing.Color.Red;
      }
      else
      {
        labelGenerationResult.ForeColor = System.Drawing.Color.Green;
      }
    }



    private void btnGenerateValues_Click( object sender, EventArgs e )
    {
      GenerateValues();
    }



  }
}
