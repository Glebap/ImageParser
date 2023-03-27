using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using TMPro;

public class ImageLoader : MonoBehaviour
{
    [SerializeField] private Image _container;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Transform _contentTransform;
    private readonly List<Image> _imageContainers = new ();
    private readonly Dictionary<string, Sprite> _imageCache = new ();

    public void OnParseButtonCLicked()
    {
        ClearContent();
        ParseImagesFromUrl(_inputField.text);
    }
    
    private void ParseImagesFromUrl(string url)
    {

        var htmlDoc = new HtmlWeb().Load(url);
        var imageTags = htmlDoc.DocumentNode.Descendants("img");
        var imageUrls = new List<string>();
        var root = GetRootUrl(url);

        foreach (var tag in imageTags)
        {
            var src = tag.Attributes["src"]?.Value;
            if (!string.IsNullOrEmpty(src))
            {
                var fullLink = src.Length > 5 && src[0..5] == "https";
                imageUrls.Add(fullLink ? src : $"{root}{src}");
                _imageContainers.Add(Instantiate(_container, _contentTransform));
            }
        }


        for (var index = 0; index < imageUrls.Count; index++)
        {
            var imageUrl = imageUrls[index];
            var container = _imageContainers[index];
            
            if (_imageCache.ContainsKey(url))
            {
                container.sprite = _imageCache[url];
                continue;
            }
            
            StartCoroutine(DownloadImage(imageUrl, container));
        }
    }

    private string GetRootUrl(string url)
    {
        var root = "";
        var slashCount = 2;
        foreach (var urlChar in url)
        {
            if (urlChar == '/' && slashCount-- == 0) return root;
            root += urlChar;
        }

        return root;
    }

    private IEnumerator DownloadImage(string MediaUrl, Image container)
    {
        using var request = UnityWebRequestTexture.GetTexture(MediaUrl);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to download image from {MediaUrl}: {request.error}");
            yield break;
        }

        var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        
        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Application.persistentDataPath, Guid.NewGuid() + ".png"), bytes);

        var savedTexture = new Texture2D(2, 2);
        savedTexture.LoadImage(bytes);
        var sprite = Sprite.Create(savedTexture, new Rect(0, 0, savedTexture.width, savedTexture.height),
            Vector2.zero);

        container.sprite = sprite;
        
        _imageCache[MediaUrl] = sprite;
    }

    private void ClearContent()
    {
        _imageContainers.Clear();
        foreach (Transform child in _contentTransform)
            Destroy(child.gameObject);
    }
}
