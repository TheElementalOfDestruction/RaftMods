using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


namespace DestinyCustomBlocks
{
    public class CustomBlocksMenu : MonoBehaviour
    {
        private static readonly string MOD_DATA_FOLDER = Path.Combine(HMLLibrary.HLib.path_modsFolder, "ModData");
        private static readonly string RESOURCE_LOCATION = Path.Combine(MOD_DATA_FOLDER, "CustomBlocks");

        private CanvasGroup cg;
        private UnityEngine.UI.Image preview;
        private ICustomBlock currentBlock;
        private TMPro.TMP_InputField inputField;
        private byte[] imageData;
        private bool shown;

        void Start()
        {
            // Setup the button events.
            foreach (var button in this.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                if (button.gameObject.name.StartsWith("CloseButton"))
                {
                    button.onClick.AddListener(this.HideMenu);
                }
                else if (button.gameObject.name.StartsWith("DefaultButton"))
                {
                    button.onClick.AddListener(this.SetBlockDefault);
                }
                else if (button.gameObject.name.StartsWith("LoadButton"))
                {
                    button.onClick.AddListener(this.LoadPreviewStart);
                }
                else if (button.gameObject.name.StartsWith("UpdateButton"))
                {
                    button.onClick.AddListener(this.UpdateBlock);
                }
            }

            // Get the components.
            this.cg = this.GetComponent<CanvasGroup>();
            this.preview = this.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.gameObject.name.StartsWith("Preview"));
            this.inputField = this.GetComponentInChildren<TMPro.TMP_InputField>();

            // Setup the text entry.
            this.inputField.onSubmit.AddListener(this.LoadPreviewStartText);

            this.HideMenu();

            // Make sure the ModData folder exists.
            if (!Directory.Exists(MOD_DATA_FOLDER))
            {
                CustomBlocks.DebugLog("ModData folder not found. Creating it manually.");
                Directory.CreateDirectory(MOD_DATA_FOLDER);
            }

            // Finally, let's setup out folder for custom blocks to be put in.
            if (!Directory.Exists(RESOURCE_LOCATION))
            {
                CustomBlocks.DebugLog("Directory for custom blocks was not found. Creating.");
                Directory.CreateDirectory(RESOURCE_LOCATION);
            }
        }

        void Update()
        {
            if (this.shown && Input.GetKeyDown("escape"))
            {
                this.HideMenu();
            }
        }

        private void HandleError(ErrorType e)
        {
            CustomBlocks.Log(e);
        }

        private void HandleError(string e)
        {
            CustomBlocks.Log(e);
        }

        public void HideMenu()
        {
            this.currentBlock = null;
            this.cg.alpha = 0;
            this.cg.interactable = false;
            this.cg.blocksRaycasts = false;
            this.shown = false;
            this.inputField.readOnly = true;
            RAPI.ToggleCursor(false);
        }

        public IEnumerator LoadPreview()
        {
            if (!this.currentBlock)
            {
                yield break;
            }
            byte[] temp = null;
            string path = this.inputField.text;

            if (path.ToLower().StartsWith("http://") || path.ToLower().StartsWith("https://"))
            {
                UnityWebRequest www = UnityWebRequest.Get(path);
                var handler = new DownloadHandlerTexture();
                www.downloadHandler = handler;

                yield return www.SendWebRequest();

                if (www.responseCode > 500)
                {
                    this.HandleError("A server error occured while getting the url.");
                }
                else if (www.responseCode > 400)
                {
                    this.HandleError("The url could not be accessed.");
                }
                else if (www.responseCode != 200)
                {
                    this.HandleError($"Request had response code of {www.responseCode}");
                }
                else if (www.error != null)
                {
                    this.HandleError(www.error);
                }
                else if (handler.texture == null)
                {
                    this.HandleError("Remote file was not a valid texture.");
                }
                else
                {
                    // Success!
                    this.imageData = handler.data.SanitizeImage(this.currentBlock.GetBlockType());
                    this.preview.overrideSprite = CustomBlocks.CreateSpriteFromBytes(this.imageData, this.currentBlock.GetBlockType());
                }
            }
            else
            {
                bool found = File.Exists(path);
                if (!found && File.Exists(Path.Combine(RESOURCE_LOCATION, path)))
                {
                    found = true;
                    path = Path.Combine(RESOURCE_LOCATION, path);
                }

                if (found)
                {
                    temp = File.ReadAllBytes(path);
                    byte[] temp2 = temp.Length > 0 ? temp.SanitizeImage(this.currentBlock.GetBlockType()) : temp;
                    Sprite s = CustomBlocks.CreateSpriteFromBytes(temp2, this.currentBlock.GetBlockType());
                    if (temp2.Length == 0 || s == null)
                    {
                        this.HandleError("File does not contain a valid flag. Valid flag must be a PNG or JPG file.");
                    }
                    else
                    {
                        this.imageData = temp2;
                        this.preview.overrideSprite = s;
                    }
                }
                else
                {
                    this.HandleError("Path could not be found.");
                }
            }
        }

        public void LoadPreviewStart()
        {
            StartCoroutine(this.LoadPreview());
        }

        public void LoadPreviewStartText(string _ = "")
        {
            StartCoroutine(this.LoadPreview());
        }

        public void SetBlockDefault()
        {
            this.imageData = new byte[0];
            this.preview.overrideSprite = CustomBlocks.instance.defaultSprites[this.currentBlock.GetBlockType()];
        }

        public void ShowMenu(ICustomBlock cb)
        {
            if (cb == null)
            {
                return;
            }
            this.cg.alpha = 1;
            this.cg.interactable = true;
            this.cg.blocksRaycasts = true;
            this.shown = true;
            this.currentBlock = cb;
            this.imageData = cb.GetImageData();
            this.inputField.readOnly = false;
            this.preview.overrideSprite = CustomBlocks.CreateSpriteFromBytes(this.imageData, cb.GetBlockType());
            RAPI.ToggleCursor(true);
        }

        public void UpdateBlock()
        {
            if (this.currentBlock != null)
            {
                this.currentBlock.SetImageData(this.imageData);
            }
            this.HideMenu();
        }

        enum ErrorType
        {
            PATH_NOT_FOUND,
            URL_NOT_FOUND,
            URL_ERROR,
            BAD_DATA
        }
    }
}
