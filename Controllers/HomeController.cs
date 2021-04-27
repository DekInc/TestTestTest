using HilmerSVGTransform.Models;
using Newtonsoft.Json;
using Svg;
using Svg.Transforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HilmerSVGTransform.Controllers {
    public class HomeController : Controller {
        public float BoundingBox { get; set; } = 0.1f;
        public float ScalePath { get; set; } = 0.1f;
        List<SvgElement> ListProcNodes { get; set; } = new List<SvgElement>();
        public ActionResult Index() {
            return View();
        }
        void FilterBoundingBox(SvgElementCollection Nodes) {
            foreach (SvgElement Node in Nodes) {
                if (Node.GetType().Name == "SvgPath") {
                    //|| Node.ID.StartsWith("ellipse")
                    //|| Node.ID.StartsWith("circle")
                    //|| Node.ID.StartsWith("line")
                    //|| Node.ID.StartsWith("polygon")
                    //|| Node.ID.StartsWith("rect")
                    //|| Node.ID.StartsWith("polyline")
                    try {
                        SvgPath PathNode = ((SvgPath)Node);
                        double Diag = Math.Sqrt(Math.Pow(PathNode.Bounds.Width, 2) + Math.Pow(PathNode.Bounds.Height, 2));
                        Debug.WriteLine(Diag);
                        if (Diag < BoundingBox) {
                            Node.Visibility = "hidden";
                            SvgTransformCollection NewTransformList = new SvgTransformCollection();
                            NewTransformList.Add(new SvgScale(0, 0));
                            Node.Transforms = NewTransformList;
                        }
                    } catch {
                    }
                }
                FilterBoundingBox(Node.Children);
            }
        }
        void DoScalePaths(SvgElementCollection Nodes) {
            foreach (SvgElement Node in Nodes) {
                if (Node.GetType().Name == "SvgPath") {
                    try {
                        if (!ListProcNodes.Contains(Node)) {
                            SvgTransformCollection NewTransformList = new SvgTransformCollection {
                                new SvgScale(ScalePath, ScalePath)
                            };
                            Node.Transforms = NewTransformList;
                            ListProcNodes.Add(Node);
                        }
                    } catch {
                    }
                }
                DoScalePaths(Node.Children);
            }
        }
        [HttpPost]
        public ActionResult ProcessSvg(string TxtAlpha, string TxtScale, string TxtScale2, string TxtBackground, string TxtBoundingBox) {
            if (Request.Files.Count > 0) {
                try {
                    HttpFileCollectionBase Files = Request.Files;
                    HttpPostedFileBase FileSvg = Files[0];
                    SvgDocument SvgFileDoc = SvgDocument.Open<SvgDocument>(FileSvg.InputStream, null);
                    if (SvgFileDoc != null) {
                        if (!string.IsNullOrEmpty(TxtScale)) {
                            float ScaleVal = 0.0f;
                            try {
                                ScaleVal = Convert.ToInt32(TxtScale) * 1.0f / 100.0f;
                            } catch {
                                return Json(new ProcessSvgRes() {
                                    Error1 = "Error in scale value"
                                });
                            }
                            if (ScaleVal != 0.0f) {
                                SvgTransformCollection NewTransformList = new SvgTransformCollection();
                                NewTransformList.Add(new SvgScale(ScaleVal, ScaleVal));
                                SvgFileDoc.Transforms = NewTransformList;
                            }
                        }
                        if (!string.IsNullOrEmpty(TxtScale2)) {
                            ScalePath = 0.0f;
                            try {
                                ScalePath = Convert.ToInt32(TxtScale2) * 1.0f / 100.0f;
                            } catch {
                                return Json(new ProcessSvgRes() {
                                    Error1 = "Error in scale value"
                                });
                            }
                            if (ScalePath != 0.0f) {
                                DoScalePaths(SvgFileDoc.Children);
                            }
                        }
                        if (!string.IsNullOrEmpty(TxtBoundingBox)) {
                            BoundingBox = -1f;
                            try {
                                BoundingBox = Convert.ToSingle(TxtBoundingBox);
                            } catch {
                                return Json(new ProcessSvgRes() {
                                    Error1 = "Error in BoundingBox value"
                                });
                            }
                            if (BoundingBox != -1f)
                                FilterBoundingBox(SvgFileDoc.Children);
                        }
                        Bitmap ImgFinal = SvgFileDoc.Draw();
                        if (!string.IsNullOrEmpty(TxtAlpha)) {
                            float Opa = 0.0f;
                            try {
                                Opa = Convert.ToInt32(TxtAlpha);
                            } catch {
                                return Json(new ProcessSvgRes() {
                                    Error1 = "Error in alpha value"
                                });
                            }
                            if (Opa != 0.0f) {
                                Bitmap RetImg = Utils.Util.SetImageOpacity(ImgFinal, Opa / 255f);
                                if (RetImg == null)
                                    return Json(new ProcessSvgRes() {
                                        Error1 = "Error setting alpha channel"
                                    });
                                else ImgFinal = RetImg;
                            }
                        }
                        if (!string.IsNullOrEmpty(TxtBackground)) {
                            Color Argb = Color.Khaki;
                            try {
                                //Argb = Convert.ToInt32(TxtBackground.Replace("#", "").Trim(), 16);
                                Argb = ColorTranslator.FromHtml(TxtBackground);
                            } catch {
                                return Json(new ProcessSvgRes() {
                                    Error1 = "error in background value"
                                });
                            }
                            if (Argb != Color.Khaki) {
                                Bitmap BackgroundBmp = new Bitmap(ImgFinal.Width, ImgFinal.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                Graphics g = Graphics.FromImage(BackgroundBmp);
                                g.FillRectangle(new SolidBrush(Argb), 0, 0, ImgFinal.Width, ImgFinal.Height);
                                Rectangle Frame = new Rectangle(0, 0, ImgFinal.Width, ImgFinal.Height);
                                g.DrawImage(ImgFinal, Frame, Frame, GraphicsUnit.Pixel);
                                ImgFinal = BackgroundBmp;
                                BackgroundBmp.Save(Server.MapPath("~\\Background.bmp"));
                            }
                        }
                        ImgFinal.Save(Server.MapPath("~\\TempRes.bmp"));
                        try {
                            using (MemoryStream TempMs2 = new MemoryStream()) {
                                ImgFinal.Save(TempMs2, System.Drawing.Imaging.ImageFormat.Png);
                                string SigBase64 = Convert.ToBase64String(TempMs2.GetBuffer());
                                return new JsonResult(){ Data = SigBase64, MaxJsonLength = Int32.MaxValue};
                            }
                        } catch {
                            return Json(new ProcessSvgRes() {
                                Error1 = "Error in final step to convert image to Base64"
                            });
                        }
                    } else {
                        return Json(new ProcessSvgRes() {
                            Error1 = "Error in parsing SVG file"
                        });
                    }
                } catch (Exception Ex) {
                    return Json(new ProcessSvgRes() {
                        Error1 = Ex.ToString() + Ex.StackTrace
                    });
                }
            } else {
                return Json(new ProcessSvgRes() {
                    Error1 = "No files"
                });
            }
        }
    }
}