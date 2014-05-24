/** @jsx React.DOM */

var GoogleLoginButton = React.createClass({
    componentDidMount: function() {
	// global scope
	signInCallback = function(authResult) {
	    console.log("google login");
	    console.dir(authResult);
	    this.props.login(authResult);
	}.bind(this);

	(function () {
	    var po = document.createElement('script');
	    po.type = 'text/javascript';
	    po.async = true;
	    po.src = 'https://plus.google.com/js/client:plusone.js?onload=start';
	    var s = document.getElementsByTagName('script')[0];
	    s.parentNode.insertBefore(po, s);
	})();
    },
    render: function(){
	return (
	    <div id="signinButton">
		<span className="g-signin"
	           data-scope="https://www.googleapis.com/auth/plus.login"
	           data-clientid="108491861456-g9oajn3u17m0dc6e0fu1o8phoeju2v1d.apps.googleusercontent.com"
	           data-redirecturi="postmessage"
	           data-accesstype="online"
	           data-cookiepolicy="single_host_origin"
	           data-callback="signInCallback">
		</span>
   	    </div>
	);
    }
});

var UserNavItem = React.createClass({
    getInitialState: function() {
	return {username: "", loggedIn: false, userImage: null};
    },
    loginSuccess: function(authResult) {
	var me = this;
	gapi.auth.setToken(authResult);
	gapi.client.load('plus','v1', function(){
	    var request = gapi.client.plus.people.list({
		'userId': 'me',
		'collection': 'visible'
	    });
	    
	    var meRequest = gapi.client.plus.people.get({'userId': 'me'});
	    meRequest.execute(function(resp) {
		console.dir(resp);
		me.setState({username: resp.name.givenName, 
			       loggedIn: true, 
			       userImage: resp.image.url});
	    });

	    request.execute(function(resp) {
		//console.log(resp);
	    });
	});
    },

    render: function(){
	if(this.state.loggedIn) {
	    return (
		<div>
		    <img src={this.state.userImage} />
 		</div>
	    );
	}
	return (
	    <div>
		<GoogleLoginButton className="margin-bar" 
	                           login={this.loginSuccess} ></GoogleLoginButton>
	    </div>
	);
    }
});

var NavigationBar = React.createClass({
    render: function() {
	return (
	    <div className="navbar navbar-default navbar-fixed-top" role="navigation">
		<div className="container">
	
		<div className="row">
  		    <div className="navbar-header">
		       <a className="navbar-brand" href="#">Deal Hunter</a>
		    </div>
		    <div className="collapse navbar-collapse">

               	       <div className="col-sm-6 col-md-6">
	                  <div className="input-group margin-bar">
		             <input type="text" className="form-control" 
		                    placeholder="Search" name="query" value="" />
		             <div className="input-group-btn">
		                <button type="submit" className="btn btn-success">
		                   <span className="glyphicon glyphicon-search"></span>
		                </button>
		             </div>
	                  </div>
	               </div>

                       <div className="col-sm-2 col-md-2">
	                 <ul className="nav navbar-nav">
		           <li className="active"><a href="#">Deals</a></li>
                         </ul>
	               </div>
	    
	               <ul className="nav navbar-nav navbar-right">
		         <UserNavItem />
	               </ul>

	            </div>
	         </div>
              </div>
           </div>
        );
    }
});
React.renderComponent(<NavigationBar />, document.getElementById('navbar'));


function init() {
    var productRoutes = crossroads.addRoute("section1", function (id) {
    });

    var default_routes = crossroads.addRoute("/section2", function (source) {
    });

    crossroads.parse(document.location.pathname); /*This is where the parser function is called to match against the routes defined*/

    crossroads.routed.add(console.log, console);
}

//setup hasher
function parseHash(newHash, oldHash){
  crossroads.parse(newHash);
}
hasher.initialized.add(parseHash); //parse initial hash
hasher.changed.add(parseHash); //parse hash changes
hasher.init(); //start listening for history change
 
//update URL fragment generating new history record
hasher.setHash('home');

;$(init);
