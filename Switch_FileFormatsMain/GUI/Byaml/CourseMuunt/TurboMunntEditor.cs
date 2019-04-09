﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Switch_Toolbox.Library.Forms;
using Switch_Toolbox.Library;
using System.Windows.Forms;
using FirstPlugin.Turbo.CourseMuuntStructs;
using GL_EditorFramework.EditorDrawables;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace FirstPlugin.Forms
{
    public partial class TurboMunntEditor : UserControl, IViewportContainer
    {
        Viewport viewport;
        GLControl2D viewport2D;

        bool IsLoaded = false;

        public TurboMunntEditor()
        {
            InitializeComponent();

            stTabControl1.myBackColor = FormThemes.BaseTheme.FormBackColor;

            treeView1.BackColor = FormThemes.BaseTheme.FormBackColor;
            treeView1.ForeColor = FormThemes.BaseTheme.FormForeColor;

            viewport = new Viewport();
            viewport.Dock = DockStyle.Fill;
            viewport.scene.SelectionChanged += Scene_SelectionChanged;
            stPanel4.Controls.Add(viewport);

            viewport2D = new GLControl2D();
            viewport2D.Dock = DockStyle.Fill;
            stPanel3.Controls.Add(viewport2D);
        }


        public Viewport GetViewport() => viewport;

        public void UpdateViewport()
        {
            if (viewport != null)
                viewport.UpdateViewport();
        }

        public AnimationPanel GetAnimationPanel() => null;


        CourseMuuntScene scene;

        public void LoadCourseInfo(System.Collections.IEnumerable by, string FilePath)
        {
            string CourseFolder = System.IO.Path.GetDirectoryName(FilePath);
            scene = new CourseMuuntScene(by);

            if (File.Exists($"{CourseFolder}/course_kcl.szs"))
                scene.AddRenderableKcl($"{CourseFolder}/course_kcl.szs");
            if (File.Exists($"{CourseFolder}/course.kcl"))
                scene.AddRenderableKcl($"{CourseFolder}/course.kcl");

            if (File.Exists($"{CourseFolder}/course_model.szs"))
            {
                scene.AddRenderableBfres($"{CourseFolder}/course_model.szs");
            }

            viewport.AddDrawable(new GL_EditorFramework.EditorDrawables.SingleObject(new OpenTK.Vector3(0)));

            viewport.LoadObjects();

            treeView1.Nodes.Add("Scene");

            if (scene.LapPaths.Count > 0) {
                AddPathDrawable("Lap Path", scene.LapPaths,Color.Blue);
            }
            if (scene.GravityPaths.Count > 0) {
                AddPathDrawable("Gravity Path", scene.GravityPaths, Color.Purple);
            }
            if (scene.EnemyPaths.Count > 0) {
                AddPathDrawable("Enemy Path", scene.EnemyPaths, Color.Red);
            }
            if (scene.GlidePaths.Count > 0) {
                AddPathDrawable("Glide Path", scene.GlidePaths, Color.Orange);
            }
            if (scene.ItemPaths.Count > 0) {
                AddPathDrawable("Item Path", scene.ItemPaths, Color.Yellow);
            }
            if (scene.PullPaths.Count > 0) {
                AddPathDrawable("Pull Path", scene.PullPaths, Color.GreenYellow);
            }
            if (scene.SteerAssistPaths.Count > 0) {
                AddPathDrawable("Steer Assist Path", scene.SteerAssistPaths, Color.Green);
            }
            if (scene.Paths.Count > 0) {
                AddPathDrawable("Path", scene.Paths, Color.Black);
            }
            if (scene.ObjPaths.Count > 0) {
              //  AddPathDrawable("Object Path", scene.ObjPaths, Color.DarkSeaGreen);
            }
            if (scene.JugemPaths.Count > 0) {
                AddPathDrawable("Jugem Path", scene.JugemPaths, Color.DarkSeaGreen);
            }
            if (scene.IntroCameras.Count > 0) {
                AddPathDrawable("IntroCamera", scene.IntroCameras, Color.Pink);
            }

            foreach (var kcl in scene.KclObjects)
            {
            //    viewport.AddDrawable(kcl.Renderer);
            //    treeView1.Nodes.Add(kcl);
             //   kcl.Checked = true;
            }

            foreach (var bfres in scene.BfresObjects)
            {
              //  viewport.AddDrawable(bfres.BFRESRender);
              //  treeView1.Nodes.Add(bfres);
             //   bfres.Checked = true;
            }

            IsLoaded = true;
        }

        private void AddPathDrawable(string Name, IEnumerable<BasePathPoint> Groups, Color color, bool CanConnect = true)
        {

        }

        private void AddPathDrawable(string Name, IEnumerable<BasePathGroup> Groups, Color color, bool CanConnect = true)
        {
            //Create a connectable object to connect each point
            var renderablePathConnected = new RenderableConnectedPaths(color);

            if (Name == "Lap Path" || Name == "Gravity Path")
                renderablePathConnected.Use4PointConnection = true;

            if (CanConnect) {
                viewport.AddDrawable(renderablePathConnected);
            }

            //Load a node wrapper to the tree
            var pathNode = new PathCollectionNode(Name);
            treeView1.Nodes.Add(pathNode);

            int groupIndex = 0;
            foreach (var group in Groups)
            {
                if (CanConnect)
                    renderablePathConnected.AddGroup(group);

                var groupNode = new PathGroupNode($"{Name} Group{groupIndex++}");
                pathNode.Nodes.Add(groupNode);

                int pointIndex = 0;
                foreach (var path in group.PathPoints)
                {
                    var pontNode = new PathPointNode($"{Name} Point{pointIndex++}");
                    pontNode.PathPoint = path;
                    groupNode.Nodes.Add(pontNode);

                    path.OnPathMoved = OnPathMoved;
                    viewport.AddDrawable(path.RenderablePoint);
                }
            }
        }

        private void OnPathMoved() {
            stPropertyGrid1.Refresh();
        }

        private void Scene_SelectionChanged(object sender, EventArgs e)
        {
            foreach (EditableObject o in viewport.scene.objects)
            {
                if (o.IsSelected() && o is RenderablePathPoint)
                {
                    stPropertyGrid1.LoadProperty(((RenderablePathPoint)o).NodeObject, OnPropertyChanged);
                }
            }
        }

        private void OnPropertyChanged()
        {

        }

        private void viewIntroCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Sort list by camera number/id
            scene.IntroCameras.Sort((x, y) => x.CameraNum.CompareTo(y.CameraNum));

            foreach (var camera in scene.IntroCameras)
            {
                var pathMove = scene.Paths[camera.Camera_Path];
                var pathLookAt = scene.Paths[camera.Camera_AtPath];

                //The time elapsed for each point
                int PathTime = camera.CameraTime / pathMove.PathPoints.Count;

                //Go through each point
                for (int p = 0; p < pathMove.PathPoints.Count; p++)
                {
                    //If lookat path is higher than the move path, break
                    if (pathLookAt.PathPoints.Count >= p)
                        break;

                    //Set our points
                    var pathMovePoint = pathMove.PathPoints[p];
                    var pathLookAtPoint = pathLookAt.PathPoints[p];

                    for (int frame = 0; frame < PathTime; frame++)
                    {
                        if (viewport.GL_ControlModern != null)
                        {
                            viewport.GL_ControlModern.CameraEye = pathLookAtPoint.Translate;
                            viewport.GL_ControlModern.CameraTarget = pathMovePoint.Translate;

                            viewport.UpdateViewport();
                        }
                    }
                }
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            List<EditableObject> newSelection = new List<EditableObject>();

            TreeNode node = treeView1.SelectedNode; 
            if (node == null)
                return;

            if (node.Text == "Scene")
            {
                stPropertyGrid1.LoadProperty(scene, OnPropertyChanged);
            }
            else if (node is PathCollectionNode)
            {
                foreach (var group in ((PathCollectionNode)node).Nodes)
                {
                    foreach (var point in ((PathGroupNode)group).Nodes)
                    {
                        newSelection.Add(((PathPointNode)point).PathPoint.RenderablePoint);
                    }
                }
            }
            else if (node is PathGroupNode)
            {
                foreach (var point in ((PathGroupNode)node).Nodes)
                {
                    newSelection.Add(((PathPointNode)point).PathPoint.RenderablePoint);
                }
            }
            else if (node is PathPointNode)
            {
                newSelection.Add(((PathPointNode)node).PathPoint.RenderablePoint);
            }

            if (newSelection.Count > 0)
            {
                viewport.scene.SelectedObjects = newSelection;
                viewport.UpdateViewport();
            }
        }

        bool IsParentChecked = false;
        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e) {
            if (!IsLoaded || IsParentChecked)
                return;

            IsParentChecked = true;
            CheckChildNodes(e.Node, e.Node.Checked);
            IsParentChecked = false; //Update viewport on the last node checked

            viewport.UpdateViewport();
        }

        private void CheckChildNodes(TreeNode node, bool IsChecked)
        {
            OnNodeChecked(node, IsChecked);
            foreach (TreeNode n in node.Nodes)
            {
                n.Checked = IsChecked;
                OnNodeChecked(n, IsChecked);
                if (n.Nodes.Count > 0)
                {
                    CheckChildNodes(n, IsChecked);
                }
            }
        }

        private void OnNodeChecked(TreeNode node, bool IsChecked)
        {
            if (node is PathPointNode)
                ((PathPointNode)node).OnChecked(IsChecked);
        }
    }
}
